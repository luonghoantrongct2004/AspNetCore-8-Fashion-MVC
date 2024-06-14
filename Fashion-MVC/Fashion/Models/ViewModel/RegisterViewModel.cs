using System.ComponentModel.DataAnnotations;

namespace App_Web.Models
{
    public class RegisterViewModel
    {

        [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
        [Display(Name ="Mật khẩu")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Password và nhập lại Password không khớp")]
        [Display(Name = "Nhập lại mật khẩu")]
        public string ConfirmPassword { get; set; }
        [Required(ErrorMessage = "Họ tên là bắt buộc")]
        [Display(Name = "Họ tên")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Email bắt buộc")]
        [EmailAddress(ErrorMessage = "Không đúng định dạng Email")]
        public string Email { get; set; }

    }
}
