
using LTSBackend.Comman.Exceptions;
using LTSBackend.Comman.Middleware;
using LTSBackend.Data;
using LTSBackend.Features.Users.Commands.DeleteUser;
using MediatR;
using Microsoft.EntityFrameworkCore;

public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand, bool>
{
    private readonly AppDbContext _context;
    private readonly ILogger<DeleteUserCommandHandler> _logger;

    public DeleteUserCommandHandler(
        AppDbContext context,
        ILogger<DeleteUserCommandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> Handle(
        DeleteUserCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deactivating user: {UserId}", request.UserID);

        // ================================================
        // 1. Find user
        // ================================================
        var user = await _context.Users
            .FirstOrDefaultAsync(x => x.UserID == request.UserID, cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("Delete failed: User not found: {UserId}", request.UserID);
            throw new NotFoundException("User not found.");
        }

        // ================================================
        // 2. Check if already deleted
        // ================================================
        if (user.IsDeleted)
        {
            _logger.LogWarning("Delete failed: User already deleted: {UserId}", request.UserID);
            throw new ValidationException(
                new System.Collections.Generic.List<string>
                { "User account is already deleted." });
        }

        // ================================================
        // 3. Perform soft delete
        // ================================================
        user.IsActive = false;
        user.IsDeleted = true;
        user.UpdatedAt = DateTime.UtcNow;

        // ================================================
        // 4. Revoke all active refresh tokens
        // ================================================
        var activeTokens = _context.RefreshTokens
            .Where(x => x.UserID == request.UserID && !x.IsRevoked)
            .ToList();

        foreach (var token in activeTokens)
        {
            token.IsRevoked = true;
        }

        _logger.LogInformation("Revoked {Count} active tokens for user: {UserId}", activeTokens.Count, request.UserID);

        // ================================================
        // 5. Save changes
        // ================================================
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User deactivated successfully: {UserId}", request.UserID);

        return true;
    }
}