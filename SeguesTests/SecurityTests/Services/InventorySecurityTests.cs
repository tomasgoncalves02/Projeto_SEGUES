using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Projeto_SEGUES.Areas.Inventory.ViewModels;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models.Inventory;
using Projeto_SEGUES.Services;

namespace SeguesTests.Services.Security
{
    public class InventorySecurityTests
    {
        private AppDbContext GetContext() =>
            new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

        [Fact]
        public async Task CreateProductAsync_PreventDuplicateName_ReturnsFail()
        {
            var context = GetContext();
            var service = new InventoryService(context, Mock.Of<ILogger<InventoryService>>());
            var cat = new ProductCategory { Id = 1, Name = "Pedro-Food", Description = "Pedro-Good" };
            context.ProductCategory.Add(cat);
            context.Product.Add(new Product { Name = "Pedro-Rice", Category = cat, Price = 1, Stock = 1, MinimumStock = 1, Description = "Pedro-Nice" });
            await context.SaveChangesAsync();

            var model = new CreateProductViewModel
            {
                Name = "Pedro-Rice",
                CategoryId = 1,
                Description = "Pedro-Good",
                Price = 1,
                Stock = 1,
                MinimumStock = 1
            };
            var result = await service.CreateProductAsync(model);

            Assert.False(result.Success);
            Assert.Equal("Já existe um produto com esse nome.", result.Message);
        }

        [Fact]
        public async Task EditProductAsync_InvalidCategory_ReturnsFail()
        {
            var context = GetContext();
            var service = new InventoryService(context, Mock.Of<ILogger<InventoryService>>());
            var model =  new CreateProductViewModel
            {
                Name = "Pedro",
                CategoryId = 1,
                Description = "Valid Description",
                Price = 1,
                Stock = 1,
                MinimumStock = 1
            };

            var result = await service.EditProductAsync(model);

            Assert.False(result.Success);
            Assert.Equal("Categoria não encontrada.", result.Message);
        }
    }
}