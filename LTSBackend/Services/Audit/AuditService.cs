using LTSBackend.Models;
using Microsoft.AspNetCore.Http;
namespace LTSBackend.Services.Audit;
public class AuditService(IHttpContextAccessor httpContextAccessor) : IAuditService
{
    public AuditLog Create(int? userId, string action)
    {
        var context = httpContextAccessor.HttpContext;
        string ip = "Unknown";
        if (context != null)
        {
            // Proxy ke peeche se real IP
            var forwarded = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(forwarded))
            {
                ip = forwarded.Split(',')[0].Trim();
            }
            else
            {
                var remoteIp = context.Connection.RemoteIpAddress;
                if (remoteIp != null)
                {
                    // ::1 ko 127.0.0.1 mein convert karo (localhost IPv6)
                    if (remoteIp.IsIPv4MappedToIPv6)
                        remoteIp = remoteIp.MapToIPv4();
                    ip = remoteIp.ToString();
                }
            }
        }
        return new AuditLog
        {
            UserID = userId,
            Action = action,
            IPAddress = ip,
            Timestamp = DateTime.UtcNow
        };
    }
}