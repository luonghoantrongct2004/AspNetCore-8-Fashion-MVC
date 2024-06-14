using System.ComponentModel.DataAnnotations;

namespace App_Web.Models
{
    public class Coupon
    {
        [Key]
        public int CouponId { get; set; }
        [Required]
        [Display(Name ="Mã giảm giá")]
        public string CouponCode { get; set; }
        [Required]
        [Display(Name = "Số tiền giảm giá")]
        [DisplayFormat(DataFormatString = "{0:N0}", ApplyFormatInEditMode = true)]
        public decimal DiscountAmount { get; set; }

        [Display(Name = "Số tiền giảm tối thiểu")]
        public int MinAmount { get; set; }
    }
}
