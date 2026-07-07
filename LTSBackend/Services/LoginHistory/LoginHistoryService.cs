using LTSBackend.Data;
using LTSBackend.Models.Security;
using Microsoft.EntityFrameworkCore;

namespace LTSBackend.Services.LoginHistory
{
    public class LoginHistoryService(AppDbContext _context, ILogger<LoginHistoryService> _logger) : ILoginHistoryService
    {
        // ===================================================
        // Login
        // ===================================================

        public async Task CreateLoginHistoryAsync(
          User user,
          string? ipAddress,
          string? userAgent,
          CancellationToken cancellationToken = default)
        {
            var history = new Models.Security.LoginHistory
            {
                UserID = user.UserID,
                LoginTime = DateTime.UtcNow,
                IPAddress = ipAddress,
                UserAgent = userAgent,
                Status = "Success",
                IsLoggedOut = false
            };

            _context.LoginHistories.Add(history);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Login history recorded for user {UserId} from {IpAddress}", user.UserID, ipAddress);
        }

        // ===================================================
        // Logout
        // ===================================================
        public async Task UpdateLogoutHistoryAsync(int userId, CancellationToken cancellationToken = default)
        {
            var history = await _context.LoginHistories
                .Where(x => x.UserID == userId && !x.IsLoggedOut)
                .OrderByDescending(x => x.LoginTime)
                .FirstOrDefaultAsync(cancellationToken);

            if (history == null)
            {
                _logger.LogWarning("No active login history found for user {UserId}", userId);
                return;
            }

            history.LogoutTime = DateTime.UtcNow;
            history.IsLoggedOut = true;
            history.Status = "Logout";

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Logout history recorded for user {UserId}", userId);
        }
    }
}