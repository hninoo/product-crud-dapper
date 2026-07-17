using ProductCrud.Web.Models;

namespace ProductCrud.Web.Repositories;

public interface IProductRepository
{
    Task<(IReadOnlyList<Product> Items, int TotalCount)> GetPagedAsync(
        string? searchTerm, int page, int pageSize);
    Task<Product?> GetByIdAsync(int productId);
    Task<int> CreateAsync(Product product);
    Task<bool> UpdateAsync(Product product);
    Task<bool> SetActiveAsync(int productId, bool isActive);
    Task<bool> DeleteAsync(int productId);
    Task<bool> NameExistsAsync(string name, int? excludingProductId = null);
}
