using LTSBackend.Models;

namespace LTSBackend.Services.Audit;

public interface IAuditService
{
    AuditLog Create(int? userId, string action);
}