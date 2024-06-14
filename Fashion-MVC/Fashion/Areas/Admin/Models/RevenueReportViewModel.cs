using System.ComponentModel.DataAnnotations;

namespace App_Web.Areas.Admin.Models
{
    public class RevenueItemViewModel
    {
        public string ProductName { get; set; }
        public int QuantitySold { get; set; }
        [DisplayFormat(DataFormatString = "{0:N0}", ApplyFormatInEditMode = true)]
        public decimal TotalPrice { get; set; }
    }

    public class RevenueReportViewModel
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public ICollection<RevenueItemViewModel> RevenueItems { get; set; }
        [DisplayFormat(DataFormatString = "{0:N0}", ApplyFormatInEditMode = true)]
        public decimal TotalRevenue { get; set; }
    }

}
