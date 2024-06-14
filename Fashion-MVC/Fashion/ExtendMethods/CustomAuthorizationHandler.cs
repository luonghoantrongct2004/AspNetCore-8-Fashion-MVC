using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using App_Web.Models;
using Microsoft.EntityFrameworkCore;

namespace App_Web.ExtendMethods
{

    public class CustomAuthorizationHandler : AuthorizationHandler<CustomAuthorizationRequirement>
    {
        private readonly AppDbContext _context;

        public CustomAuthorizationHandler(AppDbContext context)
        {
            _context = context;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, CustomAuthorizationRequirement requirement)
        {
            if (context.User.Identity.IsAuthenticated)
            {
                var userIdClaim = context.User.FindFirstValue("UserID");
                if (userIdClaim != null && int.TryParse(userIdClaim, out int userId))
                {
                    var user = await _context.Users.FindAsync(userId);
                    if (user != null)
                    {
                        foreach (var roleName in requirement.RoleNames)
                        {
                            var userRole = await _context.Roles.FirstOrDefaultAsync(r => r.Id == user.RoleId && r.Name == roleName);
                            if (userRole != null)
                            {
                                context.Succeed(requirement);
                                return;
                            }
                        }
                    }
                }
            }
            context.Fail();
        }
    }
}
