using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Projeto_SEGUES.Areas.Admin.ViewModels;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models.Inventory;
using Projeto_SEGUES.Services;

namespace SeguesTests.IntegrationTests.Services;

public class InventoryIntegrationTests
{
    private static AppDbContext GetContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task CreateProductAsync_SavesAndPersistsInDatabase()
    {
        var context = GetContext();
        var service = new InventoryService(context, Mock.Of<ILogger<InventoryService>>());
        var cat = new ProductCategory { Id = 1, Name = "Pedro-Cat", Description = "D" };
        context.ProductCategory.Add(cat);
        await context.SaveChangesAsync();

        var model = new CreateProductViewModel
        {
            Name = "Pedro-Product",
            CategoryId = 1,
            Price = 10,
            Stock = 50,
            MinimumStock = 5,
            Description = "Integration Test"
        };

        var result = await service.CreateProductAsync(model);

        var dbProduct = await context.Product.Include(p => p.Category).FirstOrDefaultAsync(p => p.Name == "Pedro-Product");
        Assert.True(result.Success);
        Assert.NotNull(dbProduct);
        Assert.Equal("Pedro-Cat", dbProduct.Category.Name);
        Assert.Equal(50, dbProduct.Stock);
    }

    [Fact]
    public async Task GetFilteredProductsAsync_VerifiesDatabaseQueryExecution()
    {
        var context = GetContext();
        var service = new InventoryService(context, Mock.Of<ILogger<InventoryService>>());
        var cat = new ProductCategory { Id = 1, Name = "Pedro-Cat", Description = "D" };

        context.Product.AddRange(
            new Product { Name = "Pedro-A", Price = 10, Stock = 10, Category = cat, MinimumStock = 1, Description = "D" },
            new Product { Name = "Pedro-B", Price = 20, Stock = 10, Category = cat, MinimumStock = 1, Description = "D" },
            new Product { Name = "Outro", Price = 5, Stock = 10, Category = cat, MinimumStock = 1, Description = "D" }
        );
        await context.SaveChangesAsync();

        var model = new InventorySearchViewModel { SearchString = "Pedro", MaxPrice = 15 };
        var result = await service.GetFilteredProductsAsync(model);

        Assert.Single(result);
        Assert.Equal("Pedro-A", result[0].Name);
    }

    [Fact]
    public async Task EditProductAsync_PersistsChangesInExistingRecord()
    {
        var context = GetContext();
        var service = new InventoryService(context, Mock.Of<ILogger<InventoryService>>());
        var cat = new ProductCategory { Id = 1, Name = "Pedro-Cat", Description = "D" };
        var product = new Product { Id = 10, Name = "Pedro-Old", Price = 5, Stock = 5, Category = cat, MinimumStock = 1, Description = "D" };
        context.ProductCategory.Add(cat);
        context.Product.Add(product);
        await context.SaveChangesAsync();

        var model = new CreateProductViewModel
        {
            Id = 10,
            Name = "Pedro-New",
            CategoryId = 1,
            Price = 99,
            Stock = 100,
            MinimumStock = 10,
            Description = "Updated",
            IsActive = true
        };

        await service.EditProductAsync(model);

        context.Entry(product).State = EntityState.Detached;
        var updatedProduct = await context.Product.FindAsync(10);

        Assert.Equal("Pedro-New", updatedProduct?.Name);
        Assert.Equal(99, updatedProduct?.Price);
        Assert.Equal(100, updatedProduct?.Stock);
    }

    [Fact]
    public async Task GetAllCategoriesForDropdownAsync_FetchesRealDataFromDb()
    {
        var context = GetContext();
        var service = new InventoryService(context, Mock.Of<ILogger<InventoryService>>());
        context.ProductCategory.AddRange(
            new ProductCategory { Id = 1, Name = "Pedro-1", Description = "D" },
            new ProductCategory { Id = 2, Name = "Pedro-2", Description = "D" }
        );
        await context.SaveChangesAsync();

        var result = await service.GetAllCategoriesForDropdownAsync();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, r => r.Text == "Pedro-1");
        Assert.Contains(result, r => r.Text == "Pedro-2");
    }

    [Fact]
    public async Task DeleteProductAsync_UpdatesIsActiveFlagInDatabase()
    {
        var context = GetContext();
        var service = new InventoryService(context, Mock.Of<ILogger<InventoryService>>());
        var product = new Product { Id = 5, Name = "Pedro-Delete", IsActive = true, Price = 1, Stock = 1, MinimumStock = 1, Description = "D", Category = new ProductCategory { 
            Name = "Nome",
            Description = "Good"
        } };
        context.Product.Add(product);
        await context.SaveChangesAsync();

        await service.DeleteProductAsync(5);

        var dbProduct = await context.Product.AsNoTracking().FirstOrDefaultAsync(p => p.Id == 5);
        Assert.False(dbProduct!.IsActive);
    }
}