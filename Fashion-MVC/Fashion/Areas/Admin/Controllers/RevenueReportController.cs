using App_Web.Areas.Admin.Models;
using App_Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace App_Web.Areas.Admin.Controllers
{
    [Authorize(Policy = "RequireOwnerRole")]
    [Area("Admin")]
    public class RevenueReportController : Controller
    {
        private readonly AppDbContext _context;

        public RevenueReportController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index(DateTime? startDate, DateTime? endDate)
        {
            if(!startDate.HasValue || !endDate.HasValue)
            {
                startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                endDate = startDate.Value.AddMonths(1).AddDays(1);
            }
            return GenerateReport(startDate.Value, endDate.Value);
        }
        [HttpPost]
        public IActionResult GenerateReport(DateTime startDate, DateTime endDate)
        {
            var revenueReport = new RevenueReportViewModel
            {
                StartDate = startDate,
                EndDate = endDate
            };

            var revenueItems = _context.Orderdetails
                                .Where(od => od.Order.OrderDate.Date >= startDate && od.Order.OrderDate.Date <= endDate)
                                .Select(od => new
                                {
                                    ProductName = od.Product.ProductName,
                                    QuantitySold = od.Quantity,
                                    TotalPrice = od.Quantity * od.Product.Price
                                })
                                .GroupBy(od => od.ProductName)
                                .Select(g => new RevenueItemViewModel
                                {
                                    ProductName = g.Key,
                                    QuantitySold = g.Sum(od => od.QuantitySold),
                                    TotalPrice = g.Sum(od => od.TotalPrice)
                                })
                                .ToList();

            revenueReport.RevenueItems = revenueItems;
            revenueReport.TotalRevenue = revenueItems.Sum(item => item.TotalPrice);

            return View(nameof(Index), revenueReport);
        }

    }
}
