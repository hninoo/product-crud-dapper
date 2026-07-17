using System.ComponentModel.DataAnnotations;
using ProductCrud.Web.Models;
using Xunit;

namespace ProductCrud.Web.Tests;

public sealed class ProductModelTests
{
    [Fact]
    public void ProductListViewModel_CalculatesPagingProperties()
    {
        var model = new ProductListViewModel
        {
            Page = 2,
            PageSize = 10,
            TotalCount = 25
        };

        Assert.Equal(3, model.TotalPages);
        Assert.True(model.HasPrevious);
        Assert.True(model.HasNext);
        Assert.Equal(11, model.FirstItem);
        Assert.Equal(20, model.LastItem);
    }

    [Fact]
    public void ProductListViewModel_HandlesEmptyResults()
    {
        var model = new ProductListViewModel
        {
            Page = 1,
            PageSize = 10,
            TotalCount = 0
        };

        Assert.Equal(1, model.TotalPages);
        Assert.False(model.HasPrevious);
        Assert.False(model.HasNext);
        Assert.Equal(0, model.FirstItem);
        Assert.Equal(0, model.LastItem);
    }

    [Fact]
    public void Product_ValidationRejectsInvalidValues()
    {
        var product = new Product
        {
            Name = string.Empty,
            Description = new string('d', 501),
            Price = -1m,
            StockQuantity = -1
        };

        var results = Validate(product);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(Product.Name)));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(Product.Description)));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(Product.Price)));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(Product.StockQuantity)));
    }

    [Fact]
    public void Product_ValidationRejectsOverlongName()
    {
        var product = new Product
        {
            Name = new string('n', 101),
            Price = 10m,
            StockQuantity = 1
        };

        var results = Validate(product);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(Product.Name)));
    }

    [Fact]
    public void Product_ValidationRejectsPriceAboveAllowedRange()
    {
        var product = new Product
        {
            Name = "Enterprise Server",
            Price = 1000000000000000m,
            StockQuantity = 1
        };

        var results = Validate(product);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(Product.Price)));
    }

    [Fact]
    public void Product_ValidationAcceptsMaximumAllowedPriceAndStock()
    {
        var product = new Product
        {
            Name = "Maximum Product",
            Price = 999999999999999.99m,
            StockQuantity = int.MaxValue
        };

        var results = Validate(product);

        Assert.Empty(results);
    }

    [Fact]
    public void Product_ValidationAcceptsValidProduct()
    {
        var product = new Product
        {
            Name = "Wireless Mouse",
            Description = "Ergonomic wireless mouse.",
            Price = 19.99m,
            StockQuantity = 10
        };

        var results = Validate(product);

        Assert.Empty(results);
    }

    private static List<ValidationResult> Validate(Product product)
    {
        var context = new ValidationContext(product);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(product, context, results, validateAllProperties: true);
        return results;
    }
}
