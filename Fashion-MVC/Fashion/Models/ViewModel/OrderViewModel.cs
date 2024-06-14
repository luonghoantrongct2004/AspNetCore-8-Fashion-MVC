using System.ComponentModel.DataAnnotations;

namespace App_Web.Models.ViewModel;

public class OrderViewModel
{
    public int OrderId { get; set; }
    public int ProductId { get; set; }  
    public int UserId { get; set; }
    public DateTime OrderDate { get; set; }
    public string Status { get; set; }
    [DisplayFormat(DataFormatString = "{0:N0}", ApplyFormatInEditMode = true)]
    public decimal TotalPrice { get; set; }
    public List<OrderItemViewModel> OrderItems { get; set; }
}
