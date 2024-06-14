using System.ComponentModel.DataAnnotations;

namespace App_Web.Models
{
    public class Introduction
    {
        public int Id { get; set; }
        [Display(Name = "Tiêu đề ")]
        public string Title { get; set; }
        [Display(Name = "Mô tả")]
        public string Description { get; set; }
        [Display(Name = "Ảnh")]
        public List<string>? ImageUrl { get; set; }
        [Display(Name = "Ngày tạo")]
        public DateTime CreatedAt { get; set; }
    }
}
