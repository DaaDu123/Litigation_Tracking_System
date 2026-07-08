using LTSBackend.Comman.Exceptions;
using LTSBackend.Data;
using LTSBackend.Services.Audit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LTSBackend.Features.Cases.Commands.DeleteCase;

public class DeleteCaseHandler : IRequestHandler<DeleteCaseCommand, bool>
{
    private readonly AppDbContext _context;
    private readonly IAuditService _auditService;
    private readonly ILogger<DeleteCaseHandler> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    private static readonly string[] ProtectedRoles =
    {
        "Admin",
        "Administrator",
        "SuperAdmin",
        "System"
    };

    public DeleteCaseHandler(
        AppDbContext context,
        IAuditService auditService,
        ILogger<DeleteCaseHandler> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _auditService = auditService;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<bool> Handle(
        DeleteCaseCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Case delete kia ja raha hai: {CaseID}", request.CaseID);

        int currentUserId = GetCurrentUserId();

        // ================================================
        // 1. Find Case
        // ================================================
        var caseToDelete = await _context.Cases.FirstOrDefaultAsync(x => x.CaseID == request.CaseID, cancellationToken);

        if (caseToDelete == null)
        {
            _logger.LogWarning("Case nahi mila: {CaseID}", request.CaseID);
            throw new NotFoundException($"Case ID {request.CaseID} nahi mila");
        }

        // ================================================
        // 2. Check agar Case archived hai
        // ================================================
        if (caseToDelete.IsArchived)
        {
            _logger.LogWarning("Archived case delete nahi ho sakta: {CaseID}", request.CaseID);
            throw new ValidationException(new List<string>
            {
                "Archived cases delete nahi ho sakte. Pehle unarchive kare"
            });
        }

        // ================================================
        // 3. Start transaction
        // ================================================
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // ================================================
            // 4. Delete related records (cascade handle karega EF Core)
            // ================================================
            _context.Cases.Remove(caseToDelete);

            // ================================================
            // 5. Create Audit Log
            // ================================================
            var auditLog = _auditService.Create(
                currentUserId,
                $"Case Delete: {caseToDelete.CaseNumber}");

            _context.AuditLogs.Add(auditLog);

            // ================================================
            // 6. Save changes
            // ================================================
            await _context.SaveChangesAsync(cancellationToken);

            // ================================================
            // 7. Commit transaction
            // ================================================
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation("Case successfully deleted: {CaseID}", request.CaseID);

            return true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Case delete fail ho gya: {CaseID}", request.CaseID);
            throw;
        }
    }
    private int GetCurrentUserId()
    {
        int currentUserId = 1; // Default fallback
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext != null)
            {
                var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userIdClaim) &&int.TryParse(userIdClaim, out var userId))
                {
                    currentUserId = userId;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get current user ID from context");
        }
        return currentUserId;
    }
}