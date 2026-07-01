using LTSBackend.Data;
using Microsoft.EntityFrameworkCore;

namespace LTSBackend.Services.BackgroundServices;

public class RefreshTokenCleanupService(IServiceScopeFactory _scopeFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _scopeFactory.CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var expiredTokens = await context.RefreshTokens
                .Where(x =>
                    x.IsRevoked ||
                    x.ExpiryDate <= DateTime.UtcNow)
                .ToListAsync(stoppingToken);

            if (expiredTokens.Count > 0)
            {
                context.RefreshTokens.RemoveRange(expiredTokens);
                await context.SaveChangesAsync(stoppingToken);
            }

            await Task.Delay(TimeSpan.FromHours(12), stoppingToken);
        }
    }
}