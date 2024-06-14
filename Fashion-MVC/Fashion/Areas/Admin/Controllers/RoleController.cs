using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using App.Areas.Admin.Models;
using App_Web.Models;

namespace App.Areas.Admin.Controllers
{
    [Authorize(Policy = "RequireOwnerRole")]
    [Area("Admin")]
    public class RoleController : Controller
    {
        private readonly AppDbContext _context;

        public RoleController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Admin/RoleControllewr
        public async Task<IActionResult> Index()
        {
            return View(await _context.Roles.ToListAsync());
        }
        public async Task<IActionResult> AddRoleForUser()
        {
            var users = await _context.Users.ToListAsync();
            var roles = await _context.Roles.ToListAsync();

            var viewModel = new AddRoleViewModel
            {
                Users = users.Select(u => new SelectListItem
                {       
                    Value = u.UserID.ToString(),
                    Text = u.FullName
                }),
                Roles = roles.Select(r => new SelectListItem
                {
                    Value = r.Id.ToString(),
                    Text = r.Name
                })
            };

            return View(viewModel);
        }
        [HttpPost]
        public async Task<IActionResult> AddRoleForUser(AddRoleViewModel model)
        {
            var user = await _context.Users.FindAsync(model.UserId);
            var role = await _context.Roles.FindAsync(model.RoleId);
            if (user != null && role != null)
            {
                user.RoleId = role.Id;
                await _context.SaveChangesAsync();
                return RedirectToAction("Role","Admin");
            }
            return View(model);
        }

    }
}
