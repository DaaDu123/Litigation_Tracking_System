using LTSBackend.Comman.Exceptions;
using LTSBackend.Data;
using LTSBackend.Services.Audit;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LTSBackend.Features.Cases.Commands.DeleteCase;

public class DeleteCaseHandler (AppDbContext _context, IAuditService _auditService, ILogger<DeleteCaseHandler> _logger) : IRequestHandler<DeleteCaseCommand, bool>
{
    public async Task<bool> Handle(DeleteCaseCommand request,CancellationToken cancellationToken)
    {
        _logger.LogInformation("Case delete kia ja raha hai: {CaseID}", request.CaseID);

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
            _context.AuditLogs.Add(
                _auditService.Create(1, $"Case Delete: {caseToDelete.CaseNumber}"));

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
}