using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Projeto_SEGUES.Areas.Admin.ViewModels;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.Inventory;
using Projeto_SEGUES.Services;

namespace SeguesTests.UnitTests.Services
{
    public class InventoryServiceUnitTests
    {
        private AppDbContext GetDatabaseContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        [Fact]
        public async Task GetProductByIdAsync_ProductExists_ReturnsProduct()
        {
            var context = GetDatabaseContext();
            var service = new InventoryService(context, new Mock<ILogger<InventoryService>>().Object);
            var product = new Product { Id = 77, Name = "Pedro-Item", Price = 10, Stock = 5, MinimumStock = 1, Description = "D", Category = new ProductCategory{
            Name = "Pessoa", Description = "Saboroso"
            } };
            context.Product.Add(product);
            await context.SaveChangesAsync();

            var result = await service.GetProductByIdAsync(77);

            Assert.NotNull(result);
            Assert.Equal("Pedro-Item", result!.Name);
        }

        [Fact]
        public async Task GetFilteredProductsAsync_FiltersByStockLevel_LowStock()
        {
            var context = GetDatabaseContext();
            var service = new InventoryService(context, new Mock<ILogger<InventoryService>>().Object);
            var cat = new ProductCategory { Id = 1, Name = "Pedro-Cat", Description = "D" };
            context.Product.AddRange(
                new Product { Name = "Pedro-Low", Stock = 1, MinimumStock = 5, Category = cat, Price = 10, IsActive = true, Description = "D" },
                new Product { Name = "Pedro-High", Stock = 10, MinimumStock = 5, Category = cat, Price = 10, IsActive = true, Description = "D" }
            );
            await context.SaveChangesAsync();

            var model = new InventorySearchViewModel { StockLevel = StockLevel.LowStock };
            var result = await service.GetFilteredProductsAsync(model);

            Assert.Single(result);
            Assert.Equal("Pedro-Low", result[0].Name);
        }

        [Fact]
        public async Task GetAllCategoriesForDropdownAsync_ReturnsMappedItems()
        {
            var context = GetDatabaseContext();
            var service = new InventoryService(context, new Mock<ILogger<InventoryService>>().Object);
            context.ProductCategory.Add(new ProductCategory { Id = 10, Name = "Pedro-Food", Description = "D" });
            await context.SaveChangesAsync();

            var result = await service.GetAllCategoriesForDropdownAsync();

            Assert.Contains(result, r => r.Text == "Pedro-Food" && r.Value == "10");
        }
    }
}