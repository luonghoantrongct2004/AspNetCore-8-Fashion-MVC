using App_Web.Models;

namespace App.Areas.Admin.Models
{
    public class DashboardViewModel
    {
        public int OrderToday { get; set; }
        public int TotalOrder { get; set; }
        public List<Order> Orders { get; set; }
        public int TotalProduct { get; set; }
        public double NewCustomerPercentage { get; set; }
        public int NewCustomerCount { get; set; }
        public String Username { get; set; }
    }
}
