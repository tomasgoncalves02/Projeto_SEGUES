using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Projeto_SEGUES.Areas.Inventory.ViewModels;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Extensions;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.Inventory;

namespace Projeto_SEGUES.Services;

public class InventoryService : IInventoryService
{
    private readonly AppDbContext _context;
    private readonly ILogger<InventoryService> _logger;

    public InventoryService(AppDbContext context, ILogger<InventoryService> logger) 
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Product?> GetProductByIdAsync(int id)
    {
        return await _context.Product.FindAsync(id);
    }

    public async Task<List<Product>> GetAvailableProductsAsync()
    {
        return await _context.Product
            .Include(p => p.Category)
            .Where(p => p.IsActive && p.Stock > 0)
            .ToListAsync();
    }

    public async Task<List<Product>> GetAllProductsAsync()
    {
        return await _context.Product
            .Include(p => p.Category)
            .ToListAsync();
    }

    public async Task<List<SelectListItem>> GetAllCategoriesForDropdownAsync()
    {
        var categories = await _context.ProductCategory.ToListAsync();
        return categories.Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name }).ToList();
    }

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
            existingProduct.Category = category!;
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