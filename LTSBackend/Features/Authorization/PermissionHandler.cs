using LTSBackend.Services.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

namespace LTSBackend.Features.Authorization;
public class PermissionHandler(IServiceScopeFactory scopeFactory): AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context,PermissionRequirement requirement)
    {
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            return;

        // Har request par naya scope — safe DbContext milega
        using var scope = scopeFactory.CreateScope();
        var permissionService = scope.ServiceProvider.GetRequiredService<IPermissionService>();

        bool hasPermission = await permissionService.HasPermissionAsync(userId, requirement.Permission);

        if (hasPermission)
            context.Succeed(requirement);
    }
}