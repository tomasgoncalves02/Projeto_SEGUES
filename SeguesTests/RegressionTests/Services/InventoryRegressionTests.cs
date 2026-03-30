using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Projeto_SEGUES.Areas.Admin.ViewModels;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models.Inventory;
using Projeto_SEGUES.Services;

namespace SeguesTests.RegressionTests.Services;

public class InventoryRegressionTests
{
    private static AppDbContext GetContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task EditProductAsync_NegativeValues_ShouldClampToZero()
    {
        var context = GetContext();
        var service = new InventoryService(context, Mock.Of<ILogger<InventoryService>>());
        var cat = new ProductCategory { Id = 1, Name = "Pedro-Cat", Description = "D" };
        context.ProductCategory.Add(cat);
        context.Product.Add(new Product { Id = 10, Name = "Pedro-Item", Price = 10, Stock = 10, Category = cat, MinimumStock = 1, Description = "Good" });
        await context.SaveChangesAsync();

        var model = new CreateProductViewModel
        {
            Id = 10,
            Name = "Pedro-Item",
            CategoryId = 1,
            Price = -5.5m,
            Stock = -10,
            MinimumStock = -1,
            Description = "Pedro-Regression-Test"
        };

        await service.EditProductAsync(model);

        var updated = await context.Product.FindAsync(10);
        Assert.Equal(0, updated?.Price);
        Assert.Equal(0, updated?.Stock);
        Assert.Equal(0, updated?.MinimumStock);
    }

    [Fact]
    public async Task EditProductAsync_KeepExistingName_ShouldBeSuccessful()
    {
        var context = GetContext();
        var service = new InventoryService(context, Mock.Of<ILogger<InventoryService>>());
        var cat = new ProductCategory { Id = 1, Name = "Pedro-Cat", Description = "D" };
        context.ProductCategory.Add(cat);
        context.Product.Add(new Product { Id = 50, Name = "Pedro-Unique", Category = cat, Price = 10, Stock = 10, MinimumStock = 1, Description = "Good" });
        await context.SaveChangesAsync();

        var model = new CreateProductViewModel
        {
            Id = 50,
            Name = "Pedro-Unique",
            CategoryId = 1,
            Price = 20,
            Stock = 20,
            MinimumStock = 1,
            Description = "Updating other fields"
        };

        var result = await service.EditProductAsync(model);

        Assert.True(result.Success);
        Assert.Equal("Produto editado com sucesso!", result.Message);
    }

    [Fact]
    public async Task DeleteProductAsync_AlreadyInactive_ShouldStayInactive()
    {
        var context = GetContext();
        var service = new InventoryService(context, Mock.Of<ILogger<InventoryService>>());
        var product = new Product { Id = 1, Name = "Pedro-Gone", IsActive = false, Price = 1, Stock = 1, MinimumStock = 1 ,Description = "Good", Category = new ProductCategory { 
            Name = "Produto",
            Description = "Good"
        } };
        context.Product.Add(product);
        await context.SaveChangesAsync();

        var result = await service.DeleteProductAsync(1);

        var dbProduct = await context.Product.FindAsync(1);
        Assert.True(result.Success);
        Assert.False(dbProduct!.IsActive);
    }

    [Fact]
    public async Task EditProductAsync_ChangeCategory_ShouldUpdateRelation()
    {
        var context = GetContext();
        var service = new InventoryService(context, Mock.Of<ILogger<InventoryService>>());
        var cat1 = new ProductCategory { Id = 1, Name = "Pedro-Old-Cat", Description = "D" };
        var cat2 = new ProductCategory { Id = 2, Name = "Pedro-New-Cat", Description = "D" };
        context.ProductCategory.AddRange(cat1, cat2);
        context.Product.Add(new Product { Id = 100, Name = "Pedro-Move", Category = cat1, Price = 1, Stock = 1, MinimumStock = 1, Description = "Good" });
        await context.SaveChangesAsync();

        var model = new CreateProductViewModel
        {
            Id = 100,
            Name = "Pedro-Move",
            CategoryId = 2,
            Price = 1,
            Stock = 1,
            MinimumStock = 1,
            Description = "Changing category"
        };

        await service.EditProductAsync(model);

        var updated = await context.Product.Include(p => p.Category).FirstOrDefaultAsync(p => p.Id == 100);
        Assert.Equal(2, updated!.Category.Id);
        Assert.Equal("Pedro-New-Cat", updated.Category.Name);
    }
}