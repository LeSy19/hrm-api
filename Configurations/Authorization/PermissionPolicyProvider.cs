// using Microsoft.AspNetCore.Authorization;
// using Microsoft.Extensions.Options;

// namespace BackendApp.Configurations.Authorization;

// public class PermissionPolicyProvider : IAuthorizationPolicyProvider
// {
//     public DefaultAuthorizationPolicyProvider FallbackPolicyProvider { get; }

//     public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
//     {
//         FallbackPolicyProvider = new DefaultAuthorizationPolicyProvider(options);
//     }

//     public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => FallbackPolicyProvider.GetDefaultPolicyAsync();
//     public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => FallbackPolicyProvider.GetFallbackPolicyAsync();

//     public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
//     {
//         // Tạo dynamic policy nếu policyName là chuỗi Permission
//         var policy = new AuthorizationPolicyBuilder();
//         policy.AddRequirements(new PermissionRequirement(policyName));
//         return Task.FromResult<AuthorizationPolicy?>(policy.Build());
//     }
// }

// public class PermissionRequirement : IAuthorizationRequirement
// {
//     public string Permission { get; }
//     public PermissionRequirement(string permission) => Permission = permission;
// }