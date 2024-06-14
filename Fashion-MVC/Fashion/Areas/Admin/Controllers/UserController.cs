using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using App.Areas.Admin.Models;
using App_Web.Models;

namespace App.Areas.Admin.Controllers
{
    [Authorize(Policy = "RequireOwnerRole")]
    [Area("Admin")]
    public class UserController : Controller
    {

        private readonly AppDbContext _context;

        public UserController(AppDbContext context)
        {
            _context = context;
        }

        //
        // GET: /ManageUser/Index
        [HttpGet]
        public async Task<IActionResult> Index(string searchString, int pageNumber = 1, int pageSize = 6)
        {
            pageNumber = Math.Max(pageNumber, 1);

            var usersQuery = _context.Users.AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                usersQuery = usersQuery.Where(t => EF.Functions.Like(t.Email, $"%{searchString}%")
                                                 || EF.Functions.Like(t.FullName, $"%{searchString}%"));
            }

            var totalUsers = await usersQuery.CountAsync();

            var users = await usersQuery
                .Include(r => r.Role)
                .OrderByDescending(t => t.UserID)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var viewModel = new PagingModel<User>
            {
                CurrentPage = pageNumber,
                TotalPages = (int)Math.Ceiling((double)totalUsers / pageSize),
                PageSize = pageSize,
                Items = users
            };

            ViewBag.CurrentFilter = searchString;

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var user = await _context.Users
                .Include(r => r.Role)
                .FirstOrDefaultAsync(u => u.UserID == id);
            if (user == null)
            {
                return NotFound();
            }
            return View(user);
        }
        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return RedirectToAction("User", "Admin");
        }
    }
}
