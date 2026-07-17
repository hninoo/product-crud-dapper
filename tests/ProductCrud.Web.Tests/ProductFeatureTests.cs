using System.Net;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ProductCrud.Web.Controllers;
using ProductCrud.Web.Models;
using ProductCrud.Web.Repositories;
using Xunit;

namespace ProductCrud.Web.Tests;

public sealed class ProductFeatureTests
{
    [Fact]
    public async Task HomePage_DisplaysProductsAndPrices()
    {
        await using var factory = new ProductCrudWebApplicationFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/");

        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Wireless Mouse", html);
        Assert.Contains("$19.99", html);
        Assert.Contains("Add product", html);
    }

    [Fact]
    public async Task ProductLifecycle_CanCreateEditAndDeleteProductThroughMvcPages()
    {
        await using var factory = new ProductCrudWebApplicationFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var createToken = await GetAntiForgeryTokenAsync(client, "/Products/Create");
        var createResponse = await client.PostAsync("/Products/Create", FormContent(
            createToken,
            ("Name", "USB Dock"),
            ("Description", "USB-C office docking station"),
            ("Price", "129.99"),
            ("StockQuantity", "7"),
            ("IsActive", "true")));

        Assert.Equal(HttpStatusCode.Redirect, createResponse.StatusCode);
        var detailsPath = createResponse.Headers.Location?.OriginalString;
        Assert.Contains("/Products/Details/3", detailsPath);

        var detailsHtml = await GetStringAsync(client, detailsPath);
        Assert.Contains("USB Dock", detailsHtml);
        Assert.Contains("$129.99", detailsHtml);

        var editToken = await GetAntiForgeryTokenAsync(client, "/Products/Edit/3");
        var editResponse = await client.PostAsync("/Products/Edit/3", FormContent(
            editToken,
            ("ProductId", "3"),
            ("Name", "USB Dock Pro"),
            ("Description", "Updated docking station"),
            ("Price", "149.99"),
            ("StockQuantity", "9"),
            ("IsActive", "true")));

        Assert.Equal(HttpStatusCode.Redirect, editResponse.StatusCode);
        Assert.NotNull(editResponse.Headers.Location);

        var indexHtml = await GetStringAsync(client, "/");
        Assert.Contains("USB Dock Pro", indexHtml);
        Assert.DoesNotContain("USB Dock</", indexHtml);
        Assert.Contains("$149.99", indexHtml);

        var deleteToken = await GetAntiForgeryTokenAsync(client, "/Products/Delete/3");
        var deleteResponse = await client.PostAsync("/Products/Delete", FormContent(
            deleteToken,
            ("id", "3")));

        Assert.Equal(HttpStatusCode.Redirect, deleteResponse.StatusCode);
        Assert.NotNull(deleteResponse.Headers.Location);

        var missingResponse = await client.GetAsync("/Products/Details/3");
        Assert.Equal(HttpStatusCode.NotFound, missingResponse.StatusCode);
    }

    [Theory]
    [InlineData("not-a-price", "3", "Price")]
    [InlineData("10000000000000000", "3", "Price")]
    [InlineData("10.00", "not-a-stock", "Stock quantity")]
    [InlineData("10.00", "2147483648", "Stock quantity")]
    public async Task Create_RejectsInvalidPriceAndStockInput(
        string price,
        string stockQuantity,
        string expectedValidationField)
    {
        await using var factory = new ProductCrudWebApplicationFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var token = await GetAntiForgeryTokenAsync(client, "/Products/Create");
        var response = await client.PostAsync("/Products/Create", FormContent(
            token,
            ("Name", $"Invalid Product {Guid.NewGuid():N}"),
            ("Description", "Should not be created"),
            ("Price", price),
            ("StockQuantity", stockQuantity),
            ("IsActive", "true")));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains(expectedValidationField, html);

        var indexHtml = await GetStringAsync(client, "/");
        Assert.DoesNotContain("Should not be created", indexHtml);
    }

    [Fact]
    public async Task Create_EncodesHtmlEnteredInNameAndDescription()
    {
        await using var factory = new ProductCrudWebApplicationFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var token = await GetAntiForgeryTokenAsync(client, "/Products/Create");
        var createResponse = await client.PostAsync("/Products/Create", FormContent(
            token,
            ("Name", "<script>alert('xss')</script>"),
            ("Description", "<b>bold description</b>"),
            ("Price", "10.00"),
            ("StockQuantity", "3"),
            ("IsActive", "true")));

        Assert.Equal(HttpStatusCode.Redirect, createResponse.StatusCode);
        var detailsPath = createResponse.Headers.Location?.OriginalString;

        var detailsHtml = await GetStringAsync(client, detailsPath);
        Assert.DoesNotContain("<script>alert('xss')</script>", detailsHtml);
        Assert.DoesNotContain("<b>bold description</b>", detailsHtml);
        Assert.Contains("&lt;script&gt;alert(&#x27;xss&#x27;)&lt;/script&gt;", detailsHtml);
        Assert.Contains("&lt;b&gt;bold description&lt;/b&gt;", detailsHtml);

        var indexHtml = await GetStringAsync(client, "/");
        Assert.DoesNotContain("<script>alert('xss')</script>", indexHtml);
        Assert.DoesNotContain("<b>bold description</b>", indexHtml);
        Assert.Contains("&lt;script&gt;alert(&#x27;xss&#x27;)&lt;/script&gt;", indexHtml);
        Assert.Contains("&lt;b&gt;bold description&lt;/b&gt;", indexHtml);
    }

    [Fact]
    public async Task Create_EncodesJavascriptEvalEnteredInNameAndDescription()
    {
        await using var factory = new ProductCrudWebApplicationFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var token = await GetAntiForgeryTokenAsync(client, "/Products/Create");
        var createResponse = await client.PostAsync("/Products/Create", FormContent(
            token,
            ("Name", "<script>eval('alert(1)')</script>"),
            ("Description", "<img src=x onerror=\"eval('alert(2)')\">"),
            ("Price", "10.00"),
            ("StockQuantity", "3"),
            ("IsActive", "true")));

        Assert.Equal(HttpStatusCode.Redirect, createResponse.StatusCode);
        var detailsPath = createResponse.Headers.Location?.OriginalString;

        var detailsHtml = await GetStringAsync(client, detailsPath);
        Assert.DoesNotContain("<script>eval('alert(1)')</script>", detailsHtml);
        Assert.DoesNotContain("<img src=x onerror=\"eval('alert(2)')\">", detailsHtml);
        Assert.Contains("&lt;script&gt;eval(&#x27;alert(1)&#x27;)&lt;/script&gt;", detailsHtml);
        Assert.Contains("&lt;img src=x onerror=&quot;eval(&#x27;alert(2)&#x27;)&quot;&gt;", detailsHtml);

        var indexHtml = await GetStringAsync(client, "/");
        Assert.DoesNotContain("<script>eval('alert(1)')</script>", indexHtml);
        Assert.DoesNotContain("<img src=x onerror=\"eval('alert(2)')\">", indexHtml);
        Assert.Contains("&lt;script&gt;eval(&#x27;alert(1)&#x27;)&lt;/script&gt;", indexHtml);
        Assert.Contains("&lt;img src=x onerror=&quot;eval(&#x27;alert(2)&#x27;)&quot;&gt;", indexHtml);
    }

    private static async Task<string> GetStringAsync(HttpClient client, string? path)
    {
        Assert.False(string.IsNullOrWhiteSpace(path));
        var response = await client.GetAsync(path);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    private static async Task<string> GetAntiForgeryTokenAsync(HttpClient client, string path)
    {
        var html = await GetStringAsync(client, path);
        var match = Regex.Match(
            html,
            "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"(?<token>[^\"]+)\"");

        Assert.True(match.Success, $"Could not find anti-forgery token on {path}.");
        return WebUtility.HtmlDecode(match.Groups["token"].Value);
    }

    private static FormUrlEncodedContent FormContent(
        string antiForgeryToken,
        params (string Key, string Value)[] values)
    {
        var formValues = new List<KeyValuePair<string, string>>
        {
            new("__RequestVerificationToken", antiForgeryToken)
        };
        formValues.AddRange(values.Select(v => new KeyValuePair<string, string>(v.Key, v.Value)));

        var content = new FormUrlEncodedContent(formValues);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
        return content;
    }

    private sealed class ProductCrudWebApplicationFactory
        : WebApplicationFactory<ProductsController>
    {
        private readonly FeatureProductRepository _repository = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration(configuration =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] =
                        "Server=(localdb)\\MSSQLLocalDB;Database=ProductCrudFeatureTests;Trusted_Connection=True;"
                });
            });

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IProductRepository>();
                services.AddSingleton<IProductRepository>(_repository);
            });
        }
    }

    private sealed class FeatureProductRepository : IProductRepository
    {
        private readonly List<Product> _products =
        [
            new()
            {
                ProductId = 1,
                Name = "Wireless Mouse",
                Description = "Ergonomic wireless mouse.",
                Price = 19.99m,
                StockQuantity = 12,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                ProductId = 2,
                Name = "Mechanical Keyboard",
                Description = "Compact mechanical keyboard.",
                Price = 59.50m,
                StockQuantity = 5,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
        ];

        private int _nextId = 3;

        public Task<(IReadOnlyList<Product> Items, int TotalCount)> GetPagedAsync(
            string? searchTerm,
            int page,
            int pageSize)
        {
            var query = _products.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(product =>
                    product.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                    || (product.Description?.Contains(
                        searchTerm,
                        StringComparison.OrdinalIgnoreCase) ?? false));
            }

            var ordered = query
                .OrderBy(product => product.Name)
                .ThenBy(product => product.ProductId)
                .ToArray();

            var items = ordered
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToArray();

            return Task.FromResult(((IReadOnlyList<Product>)items, ordered.Length));
        }

        public Task<Product?> GetByIdAsync(int productId)
        {
            var product = _products.SingleOrDefault(p => p.ProductId == productId);
            return Task.FromResult(Clone(product));
        }

        public Task<int> CreateAsync(Product product)
        {
            var newProduct = Clone(product)!;
            newProduct.ProductId = _nextId++;
            newProduct.CreatedAt = DateTime.UtcNow;
            _products.Add(newProduct);
            return Task.FromResult(newProduct.ProductId);
        }

        public Task<bool> UpdateAsync(Product product)
        {
            var existing = _products.SingleOrDefault(p => p.ProductId == product.ProductId);
            if (existing is null)
            {
                return Task.FromResult(false);
            }

            existing.Name = product.Name;
            existing.Description = product.Description;
            existing.Price = product.Price;
            existing.StockQuantity = product.StockQuantity;
            existing.IsActive = product.IsActive;
            existing.UpdatedAt = DateTime.UtcNow;
            return Task.FromResult(true);
        }

        public Task<bool> SetActiveAsync(int productId, bool isActive)
        {
            var existing = _products.SingleOrDefault(p => p.ProductId == productId);
            if (existing is null)
            {
                return Task.FromResult(false);
            }

            existing.IsActive = isActive;
            existing.UpdatedAt = DateTime.UtcNow;
            return Task.FromResult(true);
        }

        public Task<bool> DeleteAsync(int productId)
        {
            var existing = _products.SingleOrDefault(p => p.ProductId == productId);
            if (existing is null)
            {
                return Task.FromResult(false);
            }

            _products.Remove(existing);
            return Task.FromResult(true);
        }

        public Task<bool> NameExistsAsync(string name, int? excludingProductId = null)
            => Task.FromResult(_products.Any(product =>
                product.Name.Equals(name, StringComparison.OrdinalIgnoreCase)
                && (!excludingProductId.HasValue || product.ProductId != excludingProductId)));

        private static Product? Clone(Product? product)
        {
            return product is null
                ? null
                : new Product
                {
                    ProductId = product.ProductId,
                    Name = product.Name,
                    Description = product.Description,
                    Price = product.Price,
                    StockQuantity = product.StockQuantity,
                    IsActive = product.IsActive,
                    CreatedAt = product.CreatedAt,
                    UpdatedAt = product.UpdatedAt
                };
        }
    }
}
