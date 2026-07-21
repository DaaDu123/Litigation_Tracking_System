using LTSBackend.Models.Security;
using Microsoft.EntityFrameworkCore;

namespace LTSBackend.Services.LoginHistory;

public interface ILoginHistoryService
{
    Task CreateLoginHistoryAsync(User user, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default);
    Task UpdateLogoutHistoryAsync(int userId, CancellationToken cancellationToken = default);
}