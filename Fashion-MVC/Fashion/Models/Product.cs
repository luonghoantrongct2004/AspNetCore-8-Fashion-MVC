using System.ComponentModel.DataAnnotations;

namespace App_Web.Models;

public class Product
{
    public int ProductId { get; set; }

    public string ProductName { get; set; }

    public string? Description { get; set; }
    [DisplayFormat(DataFormatString = "{0:N0}", ApplyFormatInEditMode = true)]
    public decimal Price { get; set; }

    public List<string>? Images { get; set; }

    public int? CategoryId { get; set; }

    public int? BrandId { get; set; }
    public int? CommentId { get; set; }

    public int StockQuantity { get; set; }
    public int? SoldQuantity { get; set; } = 0;
    public List<string>? Color { get; set; }
    public List<string>? Size { get; set; }

    public DateTime? CreatedAt { get; set; } = DateTime.Now;
    [DisplayFormat(DataFormatString = "{0:N0}", ApplyFormatInEditMode = true)]
    public int? Discount { get; set; }

    public Category? Category { get; set; }
    public Brand? Brand { get; set; }
    public List<Comment>? Comments { get; set; }
}
