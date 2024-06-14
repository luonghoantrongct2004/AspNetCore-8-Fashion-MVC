using System.ComponentModel.DataAnnotations;

namespace App_Web.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email bắt buộc")]
        [Display(Name = "Email")]
        public string UserNameOrEmail { get; set; }


        [Required(ErrorMessage = "Password là bắt buộc")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [Display(Name = "Ghi nhớ mật khẩu?")]
        public bool RememberMe { get; set; }
    }
}
