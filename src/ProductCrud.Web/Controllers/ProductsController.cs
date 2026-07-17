using Microsoft.AspNetCore.Mvc;
using ProductCrud.Web.Models;
using ProductCrud.Web.Repositories;

namespace ProductCrud.Web.Controllers;

public sealed class ProductsController : Controller
{
    private readonly IProductRepository _productRepository;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(
        IProductRepository productRepository,
        ILogger<ProductsController> logger)
    {
        _productRepository = productRepository;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? searchTerm, int page = 1, int pageSize = 10)
    {
        if (!ProductListViewModel.AllowedPageSizes.Contains(pageSize))
        {
            pageSize = 10;
        }

        if (page < 1)
        {
            page = 1;
        }

        var (products, totalCount) = await _productRepository.GetPagedAsync(searchTerm, page, pageSize);

        var totalPages = Math.Max(1, (int)Math.Ceiling((double)totalCount / pageSize));
        if (page > totalPages)
        {
            return RedirectToAction(nameof(Index), new { searchTerm, page = totalPages, pageSize });
        }

        return View(new ProductListViewModel
        {
            Products = products,
            SearchTerm = searchTerm,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var product = await _productRepository.GetByIdAsync(id);
        return product is null ? NotFound() : View(product);
    }

    [HttpGet]
    public IActionResult Create() => View(new Product());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Product product)
    {
        if (await _productRepository.NameExistsAsync(product.Name))
        {
            ModelState.AddModelError(nameof(Product.Name),
                "A product with this name already exists.");
        }

        if (!ModelState.IsValid)
        {
            return View(product);
        }

        var newId = await _productRepository.CreateAsync(product);
        TempData["SuccessMessage"] = "Product created successfully.";
        return RedirectToAction(nameof(Details), new { id = newId });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var product = await _productRepository.GetByIdAsync(id);
        return product is null ? NotFound() : View(product);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Product product)
    {
        if (id != product.ProductId)
        {
            return BadRequest();
        }

        if (await _productRepository.NameExistsAsync(
                product.Name,
                product.ProductId))
        {
            ModelState.AddModelError(nameof(Product.Name),
                "A product with this name already exists.");
        }

        if (!ModelState.IsValid)
        {
            return View(product);
        }

        var updated = await _productRepository.UpdateAsync(product);
        if (!updated)
        {
            _logger.LogWarning(
                "Update failed because product {ProductId} was not found.",
                product.ProductId);
            return NotFound();
        }

        TempData["SuccessMessage"] = "Product updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id, bool isActive)
    {
        var updated = await _productRepository.SetActiveAsync(id, isActive);
        if (!updated)
        {
            return NotFound();
        }

        return Json(new { success = true, id, isActive });
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _productRepository.GetByIdAsync(id);
        return product is null ? NotFound() : View(product);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var deleted = await _productRepository.DeleteAsync(id);
        if (!deleted)
        {
            return NotFound();
        }

        TempData["SuccessMessage"] = "Product deleted successfully.";
        return RedirectToAction(nameof(Index));
    }
}
