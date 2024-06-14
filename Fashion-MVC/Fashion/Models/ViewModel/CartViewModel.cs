using System.ComponentModel.DataAnnotations;

namespace App_Web.Models.ViewModel
{
    public class CartViewModel
    {
        public List<CartItemViewModel> CartItems { get; set; }
        public int TotalItems { get; set; }
        public int UserId { get; set; }
        [DisplayFormat(DataFormatString = "{0:N0}", ApplyFormatInEditMode = true)]
        public decimal TotalPriceLast { get; set; }
        [DisplayFormat(DataFormatString = "{0:N0}", ApplyFormatInEditMode = true)]
        public decimal TotalPrice { get; set; }
        public List<Coupon> Coupons { get; internal set; }
        public int? CouponId { get; internal set; }
        public decimal CouponDiscountAmount { get; set; } = 0;
    }

    public class CartItemViewModel
    {
        public int CartDetailsId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public List<string>? Image {  get; set; }
        [DisplayFormat(DataFormatString = "{0:N0}", ApplyFormatInEditMode = true)]
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        [DisplayFormat(DataFormatString = "{0:N0}", ApplyFormatInEditMode = true)]
        public decimal TotalPrice { get; set; }
        public string Color { get; set; }
        public string Size { get; set; }
    }
}
