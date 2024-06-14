using Microsoft.AspNetCore.Mvc.Rendering;

namespace App.Areas.Admin.Models
{
    public class AddRoleViewModel
    {
        public int UserId { get; set; }
        public int RoleId { get; set; }

        public IEnumerable<SelectListItem> Roles { get; set; }
        public IEnumerable<SelectListItem> Users { get; set; }
    }
}
