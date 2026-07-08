using LTSBackend.Comman.Exceptions;
using LTSBackend.Comman.Middleware;
using LTSBackend.Data;
using LTSBackend.Features.Auth.ChangePassword;
using LTSBackend.Services;
using LTSBackend.Services.Audit;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LTSBackend.Features.Auth.ChangePassword;

public class ChangePasswordHandler : IRequestHandler<ChangePasswordCommand, bool>
{
    private readonly AppDbContext _context;
    private readonly IPasswordService _passwordService;
    private readonly IAuditService _auditService;
    private readonly ILogger<ChangePasswordHandler> _logger;

    public ChangePasswordHandler(AppDbContext context,IPasswordService passwordService,IAuditService auditService,ILogger<ChangePasswordHandler> logger)
    {
        _context = context;
        _passwordService = passwordService;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<bool> Handle(
        ChangePasswordCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Password change attempt for user: {UserId}", request.UserID);

        // ================================================
        // 1. Find user
        // ================================================
        var user = await _context.Users
            .FirstOrDefaultAsync(
                x => x.UserID == request.UserID,
                cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("Password change failed: User not found: {UserId}", request.UserID);
            throw new NotFoundException("User not found.");
        }

        // ================================================
        // 2. Verify old password
        // ================================================
        bool isOldPasswordValid = _passwordService.VerifyPassword(
            request.OldPassword,
            user.PasswordHash);

        if (!isOldPasswordValid)
        {
            _logger.LogWarning("Password change failed: Invalid old password for user: {UserId}", request.UserID);
            throw new ValidationException(
                new List<string> { "Old password is incorrect." });
        }

        // ================================================
        // 3. Update password
        // ================================================
        user.PasswordHash = _passwordService.HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;  // FIX: Use UpdatedAt instead of non-existent PasswordChangedDate

        // ================================================
        // 4. Create audit log
        // ================================================
        _context.AuditLogs.Add(
            _auditService.Create(user.UserID, "Password Changed"));

        // ================================================
        // 5. Save changes
        // ================================================
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Password changed successfully for user: {UserId}", request.UserID);

        return true;
    }
}