using Microsoft.AspNetCore.Authorization;

namespace App_Web.ExtendMethods
{
    public class CustomAuthorizationRequirement : IAuthorizationRequirement
    {
        public string[] RoleNames { get; }

        public CustomAuthorizationRequirement(params string[] roleNames)
        {
            RoleNames = roleNames;
        }
    }
}
