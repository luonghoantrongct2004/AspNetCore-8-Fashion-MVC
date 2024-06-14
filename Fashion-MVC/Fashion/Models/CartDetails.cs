using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace App_Web.Models
{
    public class CartDetails
    {
        [Key]
        public int CartDetailsId { get; set; }
        public int UserId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public string Size { get; set; }
        public string Color { get; set; }
        [NotMapped]
        public Product? Product { get; set; }
        public int? CouponId { get; set; }

        [NotMapped]
        [DisplayFormat(DataFormatString = "{0:N0}", ApplyFormatInEditMode = true)]
        public decimal Discount { get; set; }
        [NotMapped]
        [DisplayFormat(DataFormatString = "{0:N0}", ApplyFormatInEditMode = true)]
        public decimal CartTotal { get; set; }
        public User? User { get; set; }
    }
}
