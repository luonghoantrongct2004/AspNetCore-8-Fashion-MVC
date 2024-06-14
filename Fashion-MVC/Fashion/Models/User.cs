using System.ComponentModel.DataAnnotations;

namespace App_Web.Models
{
    public class User
    {
        public int UserID { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [RegularExpression(@"^{8,}$",
            ErrorMessage = "Password must be at least 8 characters long and contain at least one special character")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Full name is required")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; }
        [DataType(DataType.Date)]
        public DateTime CreatedDate { get; set; }
        public string? Avatar {  get; set; }
        public int? RoleId { get; set; }
        public virtual Role Role { get; set; }
    }
}
