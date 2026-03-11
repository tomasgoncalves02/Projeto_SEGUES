using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models.Inventory;

namespace Projeto_SEGUES.Services;

public class InventoryService : IInventoryService
{
    private readonly AppDbContext _context;
    
    public InventoryService(AppDbContext context) => _context = context;

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

    public async Task<ServiceResult> CreateProductAsync(Product product)
    {
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
        catch (Exception)
        {
            return ServiceResult.Fail("Ocorreu um erro ao criar o produto.");
        }
    }

    public async Task<ServiceResult> EditProductAsync(Product product)
    {
        var existingProduct = await _context.Product.FindAsync(product.Id);
        if (existingProduct == null) return ServiceResult.Fail("Produto não encontrado.");
        
        if (await _context.Product.AnyAsync(p => p.Name == product.Name && p.Id != product.Id))
        {
            return ServiceResult.Fail("Já existe um produto com esse nome.");
        }
        
        try
        {
            existingProduct.Name = product.Name;
            existingProduct.Description = product.Description;
            existingProduct.Category = product.Category;
            existingProduct.Price = product.Price;
            existingProduct.Stock = product.Stock;
            existingProduct.MinimumStock = product.MinimumStock;
            existingProduct.IsActive = product.IsActive;
            await _context.SaveChangesAsync();
            return ServiceResult.Ok("Produto editado com sucesso!");
        }
        catch (Exception)
        {
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
        catch (Exception)
        {
            return ServiceResult.Fail("Ocorreu um erro ao eliminar o produto.");
        }
    }
}