using Microsoft.EntityFrameworkCore;
using Projeto_SEGUES.Areas.Inventory.ViewModels;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models.Inventory;
using Projeto_SEGUES.Services;

namespace SeguesTests.Services
{
    public class InventoryServiceTests
    {
        private AppDbContext GetDatabaseContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        // Verifies that a product is successfully created when all data is valid
        [Fact]
        public async Task CreateProductAsync_ValidProduct_ReturnsSuccess()
        {
            var context = GetDatabaseContext();
            var service = new InventoryService(context);
            var category = new ProductCategory { Id = 1, Name = "Bebidas", Description = "Very Good" };
            context.ProductCategory.Add(category);
            await context.SaveChangesAsync();

            var model = new ProductViewModel
            {
                Name = "Sumo de Laranja",
                CategoryId = 1,
                Price = 1.50m,
                Stock = 10,
                Description = "Very Good",
                MinimumStock = 2
            };

            var result = await service.CreateProductAsync(model);

            Assert.True(result.Success);
            Assert.Equal(1, await context.Product.CountAsync());
        }

        // Prevents the creation of products with duplicate names to maintain inventory integrity
        [Fact]
        public async Task CreateProductAsync_DuplicateName_ReturnsFailure()
        {
            var context = GetDatabaseContext();
            var service = new InventoryService(context);
            var category = new ProductCategory { Id = 1, Name = "Snacks", Description = "Very Good" };
            context.ProductCategory.Add(category);
            context.Product.Add(new Product
            {
                Name = "Pedro-Snack",
                Description = "Very Good",
                Category = category,
                Price = 1.0m,
                Stock = 5,
                MinimumStock = 1
            });
            await context.SaveChangesAsync();

            var model = new ProductViewModel { Name = "Pedro-Snack", Description = "Very Good", MinimumStock = 1, Price = 20, Stock = 1, CategoryId = 1 };

            var result = await service.CreateProductAsync(model);

            Assert.False(result.Success);
            Assert.Equal("Já existe um produto com esse nome.", result.Message);
        }

        // Confirms that available products only return items that are active and have stock
        [Fact]
        public async Task GetAvailableProductsAsync_FiltersCorrectly()
        {
            var context = GetDatabaseContext();
            var service = new InventoryService(context);
            var cat = new ProductCategory { Name = "Geral", Description = "Very Good" };

            context.Product.AddRange(
                new Product { Name = "Active", Description = "Very Good", IsActive = true, Stock = 5, Category = cat, Price = 1m, MinimumStock = 1 },
                new Product { Name = "NoStock", Description = "Very Good", IsActive = true, Stock = 0, Category = cat, Price = 1m, MinimumStock = 1 },
                new Product { Name = "Inactive", Description = "Very Good", IsActive = false, Stock = 10, Category = cat, Price = 1m, MinimumStock = 1 }
            );
            await context.SaveChangesAsync();

            var result = await service.GetAvailableProductsAsync();

            Assert.Single(result);
            Assert.Equal("Active", result[0].Name);
        }

        // Validates that editing a product updates all its properties in the database
        [Fact]
        public async Task EditProductAsync_UpdatesExistingProduct()
        {
            var context = GetDatabaseContext();
            var service = new InventoryService(context);
            var cat = new ProductCategory { Id = 1, Name = "Comida", Description = "Comestivel" };
            var product = new Product { Id = 10, Name = "Antigo", Description = "Muito Pedro", Category = cat, Price = 1m, Stock = 5, MinimumStock = 1 };
            context.ProductCategory.Add(cat);
            context.Product.Add(product);
            await context.SaveChangesAsync();

            var model = new ProductViewModel
            {
                Id = 10,
                MinimumStock = 1,
                Description = "PEDROOOOOO",
                Name = "Novo Nome",
                CategoryId = 1,
                Price = 2.5m,
                Stock = 20,
                IsActive = true
            };

            var result = await service.EditProductAsync(model);

            var updated = await context.Product.FindAsync(10);
            Assert.True(result.Success);
            Assert.Equal("Novo Nome", updated!.Name);
            Assert.Equal(2.5m, updated.Price);
        }

        // Ensures that deleting a product performs a soft delete by setting IsActive to false
        [Fact]
        public async Task DeleteProductAsync_PerformsSoftDelete()
        {
            var context = GetDatabaseContext();
            var service = new InventoryService(context);
            var product = new Product
            {
                Id = 1,
                Name = "Pedro-Product",
                Description = "Very Pedro",
                IsActive = true,
                Category = new ProductCategory { Name = "X", Description = "Very Good" },
                Price = 1m,
                Stock = 1,
                MinimumStock = 1
            };
            context.Product.Add(product);
            await context.SaveChangesAsync();

            var result = await service.DeleteProductAsync(1);

            var deletedProduct = await context.Product.FindAsync(1);
            Assert.True(result.Success);
            Assert.False(deletedProduct!.IsActive);
        }
    }
}