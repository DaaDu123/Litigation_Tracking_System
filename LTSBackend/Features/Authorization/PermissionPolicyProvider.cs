using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace LTSBackend.Features.Authorization;

/// <summary>
/// Dynamically resolves an AuthorizationPolicy for any policy name that isn't
/// explicitly registered via options.AddPolicy(...). Required because
/// [HasPermission("SomePermission")] sets AuthorizeAttribute.Policy to an
/// arbitrary permission string — without this provider, ASP.NET Core's
/// default policy resolution throws:
///   InvalidOperationException: The AuthorizationPolicy named: 'X' was not found.
/// for every permission that wasn't manually pre-registered.
/// </summary>
public class PermissionPolicyProvider : IAuthorizationPolicyProvider
{
    // Falls back to the default provider for anything that IS explicitly registered
    // (e.g. built-in policies configured elsewhere via options.AddPolicy).
    public DefaultAuthorizationPolicyProvider FallbackPolicyProvider { get; }

    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    {
        FallbackPolicyProvider = new DefaultAuthorizationPolicyProvider(options);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync()=> FallbackPolicyProvider.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync()=> FallbackPolicyProvider.GetFallbackPolicyAsync();

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        // Treat every policy name coming from [HasPermission("...")] as a
        // permission check, by wrapping it in a PermissionRequirement.
        var policy = new AuthorizationPolicyBuilder().AddRequirements(new PermissionRequirement(policyName)).Build();

        return Task.FromResult<AuthorizationPolicy?>(policy);
    }
}