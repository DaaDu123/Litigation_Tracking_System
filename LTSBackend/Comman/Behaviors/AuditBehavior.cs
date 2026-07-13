using LTSBackend.Services.Audit;
using MediatR;

namespace LTSBackend.Common.Behaviors;
public class AuditBehavior<TRequest, TResponse>(IAuditService _auditService,IAuditLogService _auditLogService,   // ✅ FIX: DB me save karne ke liye ye service add ki
    ILogger<AuditBehavior<TRequest, TResponse>> _logger) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request,RequestHandlerDelegate<TResponse> next,CancellationToken cancellationToken)
    {
        // Only log commands, not queries
        bool isCommand = typeof(TRequest).Name.EndsWith("Command");
        if (isCommand)
        {
            var commandName = typeof(TRequest).Name;
            _logger.LogDebug("Executing command: {CommandName}", commandName);
        }
        try
        {
            var response = await next();
            if (isCommand)
            {
                var commandName = typeof(TRequest).Name;
                var auditLog = _auditService.Create(null, $"Command executed: {commandName}");
                // ✅ FIX: pehle sirf object bana raha tha, save nahi kar raha tha
                // ab actually DB me persist ho raha hai
                await _auditLogService.LogAsync(auditLog);
                _logger.LogDebug("Audit logged for command: {CommandName}", commandName);
            }
            return response;
        }
        catch (Exception ex)
        {
            if (isCommand)
            {
                var commandName = typeof(TRequest).Name;
                _logger.LogError(ex, "Command failed: {CommandName}", commandName);
            }
            throw;
        }
    }
}