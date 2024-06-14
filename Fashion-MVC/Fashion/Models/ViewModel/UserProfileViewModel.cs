using System.ComponentModel.DataAnnotations;

namespace App_Web.Models.ViewModel
{
    public class UserProfileViewModel
    {
        [Required(ErrorMessage = "Họ và tên là bắt buộc.")]
        [Display(Name = "Họ và tên")]
        public string? FullName { get; set; }

        [Required(ErrorMessage = "Email là bắt buộc.")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [Display(Name = "Mật khẩu hiện tại")]
        public string? CurrentPassword { get; set; }

        [Display(Name = "Mật khẩu mới")]
        public string? NewPassword { get; set; }

        [Compare("NewPassword", ErrorMessage = "Xác nhận mật khẩu không khớp.")]
        [Display(Name = "Xác nhận mật khẩu")]
        public string? ConfirmPassword { get; set; }


        public List<Order>? Orders { get; set; }
    }

}
