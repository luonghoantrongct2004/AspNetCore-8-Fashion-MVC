using System.ComponentModel.DataAnnotations;

namespace App_Web.Models.ViewModel
{
    public class CheckoutViewModel
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        [Required(ErrorMessage = "Vui lòng nhập địa chỉ giao hàng")]
        [Display(Name = "Địa chỉ giao hàng")]
        public string? ShippingAddress { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [Display(Name = "Số điện thoại")]
        public string? ContactPhone { get; set; }
        public string? PaymentMethod { get; set; }

        // Thông tin khác
        [Display(Name = "Ghi chú")]
        public string? Note { get; set; }

        // Thông tin sản phẩm trong giỏ hàng
        public List<CartDetails>? CartItems { get; set; }

        // Thông tin tổng giá tiền đơn hàng
        [DisplayFormat(DataFormatString = "{0:N0}", ApplyFormatInEditMode = true)]
        public decimal TotalPrice { get; set; }

        // ID của người dùng đặt hàng
        public int UserId { get; set; }
        public List<Product>? Products { get; set; }
        public List<ProductInfo>? ProductInfos { get; set; }
        public int? PaymentType { get; set; }
        public string? Color { get; set; }
        public string? Size { get; set; }
    }
}
