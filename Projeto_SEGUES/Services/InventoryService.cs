using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Projeto_SEGUES.Areas.Admin.ViewModels;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Extensions;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.Inventory;

namespace Projeto_SEGUES.Services;

/// <summary>
/// Service implementation for managing the bar inventory and product catalog.
/// Provides methods for CRUD operations, stock monitoring, and complex filtering.
/// </summary>
public class InventoryService : IInventoryService
{
    private readonly AppDbContext _context;
    private readonly ILogger<InventoryService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="InventoryService"/>.
    /// </summary>
    public InventoryService(AppDbContext context, ILogger<InventoryService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>Finds a product by its ID.</summary>
    public async Task<Product?> GetProductByIdAsync(int id)
    {
        return await _context.Product.FindAsync(id);
    }

    /// <summary>
    /// Retrieves active products that have stock, sorted alphabetically.
    /// Used for the customer-facing bar menu.
    /// </summary>
    public async Task<List<Product>> GetAvailableProductsAsync()
    {
        return await _context.Product
            .Include(p => p.Category)
            .Where(p => p.IsActive && p.Stock > 0)
            .OrderBy(p => p.Name)
            .ToListAsync();
    }

    /// <summary>Retrieves all products including inactive ones for administrative lists.</summary>
    public async Task<List<Product>> GetAllProductsAsync()
    {
        return await _context.Product
            .Include(p => p.Category)
            .OrderBy(p => p.Name)
            .ToListAsync();
    }

    /// <summary>
    /// Executes a complex filtered query on the product table.
    /// Handles text search, category matching, price caps, and stock level enums.
    /// </summary>
    public async Task<List<Product>> GetFilteredProductsAsync(InventorySearchViewModel model)
    {
        var query = _context.Product
            .Include(p => p.Category)
            .AsNoTracking()
            .AsQueryable();

        // Filter by Name (Case-insensitive)
        if (!string.IsNullOrWhiteSpace(model.SearchString))
        {
            var searchLower = model.SearchString.ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(searchLower));
        }

        // Filter by Category
        if (model.CategoryId is > 0)
        {
            query = query.Where(p => p.Category.Id == model.CategoryId.Value);
        }

        // Filter by Price Cap
        if (model.MaxPrice is > 0)
        {
            query = query.Where(p => p.Price <= model.MaxPrice.Value);
        }

        // Filter by Stock Status (In Stock, Low, Out of Stock)
        if (model.StockLevel.HasValue)
        {
            query = model.StockLevel.Value switch
            {
                StockLevel.InStock => query.Where(p => p.Stock > 0),
                StockLevel.LowStock => query.Where(p => p.Stock > 0 && p.Stock < p.MinimumStock),
                StockLevel.OutOfStock => query.Where(p => p.Stock == 0),
                _ => query
            };
        }

        // Optional: Active products toggle
        if (model.ActiveOnly)
        {
            query = query.Where(p => p.IsActive);
        }

        return await query.OrderBy(p => p.Name).ToListAsync();
    }

    /// <summary>Retrieves categories for dropdown population.</summary>
    public async Task<List<SelectListItem>> GetAllCategoriesForDropdownAsync()
    {
        var categories = await _context.ProductCategory.ToListAsync();
        return categories.Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name }).ToList();
    }

    /// <summary>
    /// Validates and creates a new product. 
    /// Prevents duplicate names and ensures category integrity.
    /// </summary>
    public async Task<ServiceResult> CreateProductAsync(CreateProductViewModel createProductViewModel)
    {
        var category = await _context.ProductCategory.FindAsync(createProductViewModel.CategoryId);
        if (category == null) return ServiceResult.Fail("Categoria não encontrada.");

        Product product = new Product
        {
            Name = createProductViewModel.Name,
            Description = createProductViewModel.Description,
            Category = category,
            Price = createProductViewModel.Price,
            Stock = createProductViewModel.Stock,
            MinimumStock = createProductViewModel.MinimumStock,
            IsActive = true
        };

        if (await _context.Product.AnyAsync(p => p.Name == product.Name))
        {
            return ServiceResult.Fail("Já existe um produto com esse nome.");
        }

        try
        {
            await _context.Product.AddAsync(product);
            await _context.SaveChangesAsync();
            return ServiceResult.Ok("Produto criado com sucesso!");
        }
        catch (Exception ex)
        {
            _logger.LogAppError(AppErrors.ProductCreateError, TableName.Product, AppOperation.Create, ex);
            return ServiceResult.Fail();
        }
    }

    /// <summary>
    /// Updates an existing product's details and stock parameters.
    /// Validates unique name constraints excluding the current product ID.
    /// </summary>
    public async Task<ServiceResult> EditProductAsync(CreateProductViewModel createProductViewModel)
    {
        var category = await _context.ProductCategory.FindAsync(createProductViewModel.CategoryId);
        if (category == null) return ServiceResult.Fail("Categoria não encontrada.");

        var existingProduct = await _context.Product.FindAsync(createProductViewModel.Id);
        if (existingProduct == null) return ServiceResult.Fail("Produto não encontrado.");

        if (await _context.Product.AnyAsync(p => p.Name == createProductViewModel.Name && p.Id != createProductViewModel.Id))
        {
            return ServiceResult.Fail("Já existe um produto com esse nome.");
        }

        try
        {
            existingProduct.Name = createProductViewModel.Name;
            existingProduct.Description = createProductViewModel.Description;
            existingProduct.Category = category;
            existingProduct.Price = createProductViewModel.Price >= 0 ? createProductViewModel.Price : 0;
            existingProduct.Stock = createProductViewModel.Stock >= 0 ? createProductViewModel.Stock : 0;
            existingProduct.MinimumStock = createProductViewModel.MinimumStock >= 0 ? createProductViewModel.MinimumStock : 0;
            existingProduct.IsActive = createProductViewModel.IsActive;

            await _context.SaveChangesAsync();
            return ServiceResult.Ok("Produto editado com sucesso!");
        }
        catch (Exception ex)
        {
            _logger.LogAppError(AppErrors.ProductEditError, TableName.Product, AppOperation.Update, ex);
            return ServiceResult.Fail("Ocorreu um erro ao editar o produto.");
        }
    }

    /// <summary>
    /// Implements a Soft Delete by setting IsActive to false.
    /// This preserves relational integrity for past orders.
    /// </summary>
    public async Task<ServiceResult> DeleteProductAsync(int id)
    {
        var product = await _context.Product.FindAsync(id);
        if (product == null) return ServiceResult.Fail("Produto não encontrado.");

        try
        {
            product.IsActive = false;
            await _context.SaveChangesAsync();
            return ServiceResult.Ok("Produto eliminado com sucesso!");
        }
        catch (Exception ex)
        {
            _logger.LogAppError(AppErrors.ProductDeleteError, TableName.Product, AppOperation.Delete, ex);
            return ServiceResult.Fail("Ocorreu um erro ao eliminar o produto.");
        }
    }

    /// <summary>Restores an inactive product to the active catalog.</summary>
    public async Task<ServiceResult> ReactivateProductAsync(int id)
    {
        var product = await _context.Product.FindAsync(id);
        if (product == null) return ServiceResult.Fail("Produto não encontrado.");

        try
        {
            product.IsActive = true;
            await _context.SaveChangesAsync();
            return ServiceResult.Ok("Produto reativado com sucesso!");
        }
        catch (Exception ex)
        {
            _logger.LogAppError(AppErrors.ProductEditError, TableName.Product, AppOperation.Update, ex);
            return ServiceResult.Fail("Ocorreu um erro ao reativar o produto.");
        }
    }
}