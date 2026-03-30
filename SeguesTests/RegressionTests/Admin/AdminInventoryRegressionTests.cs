using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using Moq;
using Projeto_SEGUES.Areas.Admin.ViewModels;
using Projeto_SEGUES.Areas.Inventory.ViewModels;
using Projeto_SEGUES.Models.Inventory;
using Projeto_SEGUES.Services;
using SeguesTests.Helpers;
using Xunit;

namespace SeguesTests.RegressionTests.Admin
{
    public class AdminInventoryRegressionTests
    {
        [Fact]
        public async Task CreateProductAsync_ShouldPersistExactData_WhenModelIsValid()
        {
            var (context, _, _) = MockHelper.GetIdentitySetup();
            var service = new InventoryService(context, Mock.Of<ILogger<InventoryService>>());

            var cat = new ProductCategory { Name = "Bar", Description = "D" };
            context.ProductCategory.Add(cat);
            await context.SaveChangesAsync();

            var model = new CreateProductViewModel
            {
                Name = "Pedro's Special Coffee",
                Description = "High quality caffeine",
                CategoryId = cat.Id,
                Price = 1.50m,
                Stock = 100,
                MinimumStock = 10,
                IsActive = true
            };

            var result = await service.CreateProductAsync(model);

            Assert.True(result.Success);
            var product = context.Product.FirstOrDefault(p => p.Name == "Pedro's Special Coffee");
            Assert.NotNull(product);
            Assert.Equal(1.50m, product.Price);
            Assert.Equal(100, product.Stock);
        }

        [Fact]
        public async Task EditProductAsync_ShouldUpdateAllFields_ExceptId()
        {
            var (context, _, _) = MockHelper.GetIdentitySetup();
            var service = new InventoryService(context, Mock.Of<ILogger<InventoryService>>());

            var cat = new ProductCategory { Name = "Salgados", Description = "D" };
            var product = new Product
            {
                Name = "Pedro's Old Sandwich",
                Description = "Old",
                Price = 1.00m,
                Stock = 10,
                MinimumStock = 2,
                IsActive = true,
                Category = cat
            };
            context.Product.Add(product);
            await context.SaveChangesAsync();

            var updateModel = new CreateProductViewModel
            {
                Id = product.Id,
                Name = "Pedro's New Sandwich",
                Description = "Fresh",
                CategoryId = cat.Id,
                Price = 2.00m,
                Stock = 50,
                MinimumStock = 5,
                IsActive = true
            };

            await service.EditProductAsync(updateModel);

            var updated = await context.Product.FindAsync(product.Id);
            Assert.Equal("Pedro's New Sandwich", updated.Name);
            Assert.Equal(2.00m, updated.Price);
            Assert.Equal(50, updated.Stock);
        }

        [Fact]
        public async Task DeleteProductAsync_ShouldPerformSoftDelete_Only()
        {
            var (context, _, _) = MockHelper.GetIdentitySetup();
            var service = new InventoryService(context, Mock.Of<ILogger<InventoryService>>());

            var product = new Product
            {
                Name = "Pedro's Limited Item",
                Description = "D",
                Price = 10,
                Stock = 5,
                MinimumStock = 1,
                IsActive = true,
                Category = new ProductCategory { Name = "C", Description = "D" }
            };
            context.Product.Add(product);
            await context.SaveChangesAsync();

            await service.DeleteProductAsync(product.Id);

            var dbProduct = await context.Product.FindAsync(product.Id);
            Assert.NotNull(dbProduct);
            Assert.False(dbProduct.IsActive);
        }

        [Fact]
        public async Task GetFilteredProductsAsync_ShouldReturnEmpty_WhenNoMatchFound()
        {
            var (context, _, _) = MockHelper.GetIdentitySetup();
            var service = new InventoryService(context, Mock.Of<ILogger<InventoryService>>());

            var search = new InventorySearchViewModel {  };

            var result = await service.GetFilteredProductsAsync(search);

            Assert.Empty(result);
        }
    }
}