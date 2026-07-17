using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging.Abstractions;
using ProductCrud.Web.Controllers;
using ProductCrud.Web.Models;
using ProductCrud.Web.Repositories;
using Xunit;

namespace ProductCrud.Web.Tests;

public sealed class ProductsControllerTests
{
    [Fact]
    public async Task Index_NormalizesInvalidPagingAndReturnsViewModel()
    {
        var products = new[]
        {
            new Product { ProductId = 1, Name = "Mouse", Price = 19.99m },
            new Product { ProductId = 2, Name = "Keyboard", Price = 59.50m }
        };
        var repository = new FakeProductRepository
        {
            PagedResult = (products, 2)
        };
        var controller = CreateController(repository);

        var result = await controller.Index("  key  ", page: -2, pageSize: 99);

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<ProductListViewModel>(viewResult.Model);
        Assert.Equal(1, repository.LastPage);
        Assert.Equal(10, repository.LastPageSize);
        Assert.Equal("  key  ", repository.LastSearchTerm);
        Assert.Equal(1, model.Page);
        Assert.Equal(10, model.PageSize);
        Assert.Equal(2, model.TotalCount);
        Assert.Equal(products, model.Products);
    }

    [Fact]
    public async Task Index_RedirectsWhenRequestedPageExceedsTotalPages()
    {
        var repository = new FakeProductRepository
        {
            PagedResult = (Array.Empty<Product>(), 12)
        };
        var controller = CreateController(repository);

        var result = await controller.Index("mouse", page: 9, pageSize: 5);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(ProductsController.Index), redirect.ActionName);
        Assert.Equal("mouse", redirect.RouteValues?["searchTerm"]);
        Assert.Equal(3, redirect.RouteValues?["page"]);
        Assert.Equal(5, redirect.RouteValues?["pageSize"]);
    }

    [Fact]
    public async Task Details_ReturnsViewForExistingProduct()
    {
        var product = new Product { ProductId = 7, Name = "Monitor", Price = 219m };
        var controller = CreateController(new FakeProductRepository
        {
            ProductsById = { [product.ProductId] = product }
        });

        var result = await controller.Details(product.ProductId);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Same(product, viewResult.Model);
    }

    [Fact]
    public async Task Details_ReturnsNotFoundForMissingProduct()
    {
        var controller = CreateController(new FakeProductRepository());

        var result = await controller.Details(404);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Create_ReturnsValidationErrorWhenProductNameAlreadyExists()
    {
        var product = new Product { Name = "Mouse", Price = 19.99m };
        var repository = new FakeProductRepository { NameExists = true };
        var controller = CreateController(repository);

        var result = await controller.Create(product);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Same(product, viewResult.Model);
        Assert.False(controller.ModelState.IsValid);
        Assert.True(controller.ModelState.ContainsKey(nameof(Product.Name)));
        Assert.Equal(0, repository.CreateCallCount);
    }

    [Fact]
    public async Task Create_CreatesProductAndRedirectsToDetailsWhenValid()
    {
        var product = new Product { Name = "Mouse", Price = 19.99m };
        var repository = new FakeProductRepository { CreatedId = 42 };
        var controller = CreateController(repository);

        var result = await controller.Create(product);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(ProductsController.Details), redirect.ActionName);
        Assert.Equal(42, redirect.RouteValues?["id"]);
        Assert.Same(product, repository.CreatedProduct);
        Assert.Equal("Product created successfully.", controller.TempData["SuccessMessage"]);
    }

    [Fact]
    public async Task Edit_ReturnsBadRequestWhenRouteIdAndModelIdDiffer()
    {
        var controller = CreateController(new FakeProductRepository());
        var product = new Product { ProductId = 2, Name = "Mouse", Price = 19.99m };

        var result = await controller.Edit(1, product);

        Assert.IsType<BadRequestResult>(result);
    }

    [Fact]
    public async Task Edit_ReturnsNotFoundWhenUpdateTargetIsMissing()
    {
        var product = new Product { ProductId = 3, Name = "Mouse", Price = 19.99m };
        var controller = CreateController(new FakeProductRepository
        {
            UpdateResult = false
        });

        var result = await controller.Edit(product.ProductId, product);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task DeleteConfirmed_RedirectsToIndexAfterSuccessfulDelete()
    {
        var repository = new FakeProductRepository { DeleteResult = true };
        var controller = CreateController(repository);

        var result = await controller.DeleteConfirmed(5);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(ProductsController.Index), redirect.ActionName);
        Assert.Equal(5, repository.DeletedProductId);
        Assert.Equal("Product deleted successfully.", controller.TempData["SuccessMessage"]);
    }

    [Fact]
    public async Task DeleteConfirmed_ReturnsNotFoundWhenDeleteTargetIsMissing()
    {
        var controller = CreateController(new FakeProductRepository
        {
            DeleteResult = false
        });

        var result = await controller.DeleteConfirmed(5);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task ToggleActive_ReturnsJsonWhenUpdateSucceeds()
    {
        var repository = new FakeProductRepository { SetActiveResult = true };
        var controller = CreateController(repository);

        var result = await controller.ToggleActive(8, false);

        var json = Assert.IsType<JsonResult>(result);
        AssertJsonProperty(json.Value, "success", true);
        AssertJsonProperty(json.Value, "id", 8);
        AssertJsonProperty(json.Value, "isActive", false);
        Assert.Equal(8, repository.SetActiveProductId);
        Assert.False(repository.SetActiveValue);
    }

    [Fact]
    public async Task ToggleActive_ReturnsNotFoundWhenUpdateFails()
    {
        var controller = CreateController(new FakeProductRepository
        {
            SetActiveResult = false
        });

        var result = await controller.ToggleActive(8, true);

        Assert.IsType<NotFoundResult>(result);
    }

    private static ProductsController CreateController(FakeProductRepository repository)
    {
        var controller = new ProductsController(
            repository,
            NullLogger<ProductsController>.Instance);

        controller.TempData = new TempDataDictionary(
            new DefaultHttpContext(),
            new TestTempDataProvider());

        return controller;
    }

    private static void AssertJsonProperty<T>(
        object? value,
        string propertyName,
        T expected)
    {
        Assert.NotNull(value);
        var property = value.GetType().GetProperty(propertyName);
        Assert.NotNull(property);
        Assert.Equal(expected, Assert.IsType<T>(property.GetValue(value)));
    }

    private sealed class FakeProductRepository : IProductRepository
    {
        public (IReadOnlyList<Product> Items, int TotalCount) PagedResult { get; init; }
            = (Array.Empty<Product>(), 0);

        public Dictionary<int, Product> ProductsById { get; } = [];
        public string? LastSearchTerm { get; private set; }
        public int LastPage { get; private set; }
        public int LastPageSize { get; private set; }
        public bool NameExists { get; init; }
        public int CreatedId { get; init; } = 1;
        public Product? CreatedProduct { get; private set; }
        public int CreateCallCount { get; private set; }
        public bool UpdateResult { get; init; } = true;
        public bool SetActiveResult { get; init; } = true;
        public int SetActiveProductId { get; private set; }
        public bool SetActiveValue { get; private set; }
        public bool DeleteResult { get; init; } = true;
        public int DeletedProductId { get; private set; }

        public Task<(IReadOnlyList<Product> Items, int TotalCount)> GetPagedAsync(
            string? searchTerm,
            int page,
            int pageSize)
        {
            LastSearchTerm = searchTerm;
            LastPage = page;
            LastPageSize = pageSize;
            return Task.FromResult(PagedResult);
        }

        public Task<Product?> GetByIdAsync(int productId)
        {
            ProductsById.TryGetValue(productId, out var product);
            return Task.FromResult(product);
        }

        public Task<int> CreateAsync(Product product)
        {
            CreateCallCount++;
            CreatedProduct = product;
            return Task.FromResult(CreatedId);
        }

        public Task<bool> UpdateAsync(Product product) => Task.FromResult(UpdateResult);

        public Task<bool> SetActiveAsync(int productId, bool isActive)
        {
            SetActiveProductId = productId;
            SetActiveValue = isActive;
            return Task.FromResult(SetActiveResult);
        }

        public Task<bool> DeleteAsync(int productId)
        {
            DeletedProductId = productId;
            return Task.FromResult(DeleteResult);
        }

        public Task<bool> NameExistsAsync(string name, int? excludingProductId = null)
            => Task.FromResult(NameExists);
    }

    private sealed class TestTempDataProvider : ITempDataProvider
    {
        private readonly Dictionary<string, object> _values = [];

        public IDictionary<string, object> LoadTempData(HttpContext context) => _values;

        public void SaveTempData(HttpContext context, IDictionary<string, object> values)
        {
            _values.Clear();
            foreach (var value in values)
            {
                _values[value.Key] = value.Value;
            }
        }
    }
}
