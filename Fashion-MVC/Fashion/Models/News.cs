using System.ComponentModel.DataAnnotations;

namespace App_Web.Models
{
    public class News
    {
        public int Id { get; set; }
        [Display(Name ="Tiêu đề ")]
        public string Title { get; set; }
        [Display(Name = "Nội dung")]
        public string Content { get; set; }
        [Display(Name = "Người đăng")]
        public string Author { get; set; }
        public List<string>? Image { get; set; }
        public DateTime PublishedAt { get; set; }
    }
}
