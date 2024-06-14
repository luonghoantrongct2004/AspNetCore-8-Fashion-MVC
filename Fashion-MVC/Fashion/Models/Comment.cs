using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace App_Web.Models
{
    public class Comment
    {
        public int Id { get; set; }
        public int UserId { get; set; }

        public string? Content { get; set; }

        public DateTime PostedAt { get; set; }
        public int ProductId { get; set; }
        public decimal StarRating { get; set; }
        public User User { get; set; }
    }
}
