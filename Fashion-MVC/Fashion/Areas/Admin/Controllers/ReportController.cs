using App_Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using App.Areas.Admin.Models;
using Microsoft.EntityFrameworkCore;

namespace App.Areas.Report.Controllers
{
    [Authorize(Policy = "RequireAnyRole")]
    [Area("Admin")]
    public class ReportController : Controller
	{
		private readonly AppDbContext _context;

		public ReportController(AppDbContext context)
		{
			_context = context;
		}

		public IActionResult Index()
		{
            // Lấy ngày hôm nay
            DateTime today = DateTime.Today;
            var orders = _context.Orders
                .Include(u => u.User)
                .Include(o=>o.OrderDetails)
                .ToList();
            var newUsersToday = _context.Users.ToList();
            int orderToday = _context.Orders
            .Count(tr => tr.CreatedAt.Date == DateTime.Today);

            int totalOrder = _context.Orders.Count();
            int totalProduct = _context.Products.Count();
            // Đếm số lượng người dùng mới
            int newCustomerCount = newUsersToday.Count;

            // Tính tổng số lượng người dùng
            int totalUsers = _context.Users.Count();
            // Lấy số lượng người dùng mới hôm nay và hôm qua
            int newUsersTodayCount = _context.Users.Count(u => u.CreatedDate.Date == DateTime.Today);
            /* int newUsersYesterdayCount = _context.Users.Count(u => u.CreatedDate.Date == DateTime.Today.AddDays(-1));
 */
            // Tính phần trăm sự thay đổi so với hôm qua
            double percentChange = totalUsers != 0
                ? percentChange = Math.Round((((double)newUsersTodayCount - totalUsers) / totalUsers) * 100, 2)
                : 0;
            percentChange = percentChange > 0 ? percentChange : 0;
            var user = _context.Users.FirstOrDefault();
            var model = new DashboardViewModel
            {
                OrderToday = orderToday,
                TotalOrder = totalOrder,
                Orders = orders,
                TotalProduct = totalProduct,
                NewCustomerCount = newCustomerCount,
                NewCustomerPercentage = percentChange,
                Username = user.FullName
            };

            return View(model);
        }
	}
}
