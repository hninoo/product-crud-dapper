using System.ComponentModel.DataAnnotations;

namespace ProductCrud.Web.Models;

public sealed class Product
{
    public int ProductId { get; set; }

    [Required]
    [StringLength(100)]
    [Display(Name = "Product name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Range(0, 9999999999999999.99)]
    [DataType(DataType.Currency)]
    public decimal Price { get; set; }

    [Range(0, int.MaxValue)]
    [Display(Name = "Stock quantity")]
    public int StockQuantity { get; set; }

    public bool IsActive { get; set; } = true;

    [Display(Name = "Created at")]
    public DateTime CreatedAt { get; set; }

    [Display(Name = "Updated at")]
    public DateTime? UpdatedAt { get; set; }
}
