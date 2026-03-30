using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Projeto_SEGUES.Areas.Admin.ViewModels;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.Inventory;
using Projeto_SEGUES.Services;

namespace SeguesTests.UnitTests.Services;

public class InventoryServiceUnitTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly InventoryService _service;

    public InventoryServiceUnitTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);
        _service = new InventoryService(_context, new Mock<ILogger<InventoryService>>().Object);
    }
    
    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose(); 
    }

    [Fact]
    public async Task GetProductByIdAsync_ProductExists_ReturnsProduct()
    {
        var product = new Product { Id = 77, Name = "Pedro-Item", Price = 10, Stock = 5, MinimumStock = 1, Description = "D", Category = new ProductCategory{
            Name = "Pessoa", Description = "Saboroso"
        } };
        _context.Product.Add(product);
        await _context.SaveChangesAsync();

        var result = await _service.GetProductByIdAsync(77);

        Assert.NotNull(result);
        Assert.Equal("Pedro-Item", result.Name);
    }

    [Fact]
    public async Task GetFilteredProductsAsync_FiltersByStockLevel_LowStock()
    {
        var cat = new ProductCategory { Id = 1, Name = "Pedro-Cat", Description = "D" };
        _context.Product.AddRange(
            new Product { Name = "Pedro-Low", Stock = 1, MinimumStock = 5, Category = cat, Price = 10, IsActive = true, Description = "D" },
            new Product { Name = "Pedro-High", Stock = 10, MinimumStock = 5, Category = cat, Price = 10, IsActive = true, Description = "D" }
        );
        await _context.SaveChangesAsync();

        var model = new InventorySearchViewModel { StockLevel = StockLevel.LowStock };
        var result = await _service.GetFilteredProductsAsync(model);

        Assert.Single(result);
        Assert.Equal("Pedro-Low", result[0].Name);
    }

    [Fact]
    public async Task GetAllCategoriesForDropdownAsync_ReturnsMappedItems()
    {
        _context.ProductCategory.Add(new ProductCategory { Id = 10, Name = "Pedro-Food", Description = "D" });
        await _context.SaveChangesAsync();

        var result = await _service.GetAllCategoriesForDropdownAsync();

        Assert.Contains(result, r => r is { Text: "Pedro-Food", Value: "10" });
    }
}