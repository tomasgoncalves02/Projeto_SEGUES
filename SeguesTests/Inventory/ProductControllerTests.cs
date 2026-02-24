using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using Projeto_SEGUES.Areas.Inventory.Controllers;
using Projeto_SEGUES.Areas.Inventory.ViewModels;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models.Inventory;
using Xunit;

namespace SeguesTests.Inventory
{
    public class ProductControllerTests
    {
        private AppDbContext GetDatabaseContext() => new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

        private async Task<ProductCategory> SeedCategory(AppDbContext context)
        {
            var category = new ProductCategory { Name = "Bebidas" };
            context.ProductCategories.Add(category);
            await context.SaveChangesAsync();
            return category;
        }

        private void SetupTempData(ProductController controller)
        {
            var tempData = new Mock<ITempDataDictionary>();
            controller.TempData = tempData.Object;
        }

        [Fact]
        public async Task Index_ReturnsViewWithProducts()
        {
            using var context = GetDatabaseContext();
            var controller = new ProductController(context);
            var category = await SeedCategory(context);

            context.Products.Add(new Product { Name = "Cola", Description = "Lata 33cl", Category = category });
            await context.SaveChangesAsync();

            var result = await controller.Index();

            var viewResult = Assert.IsType<ViewResult>(result);
            var products = Assert.IsAssignableFrom<IEnumerable<Product>>(controller.ViewBag.Products);
            Assert.Single(products);
        }

        [Fact]
        public async Task Create_ValidProduct_RedirectsToIndex()
        {
            using var context = GetDatabaseContext();
            var controller = new ProductController(context);
            SetupTempData(controller);

            var model = new BarProductViewModel
            {
                Product = new Product { Name = "Agua", Description = "Garrafa 50cl", Price = 1.0m, Stock = 100 }
            };

            var result = await controller.Create(model);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            Assert.Equal(1, await context.Products.CountAsync());
        }

        [Fact]
        public async Task Delete_ExistingProduct_RemovesFromDb()
        {
            using var context = GetDatabaseContext();
            var controller = new ProductController(context);
            SetupTempData(controller);

            var product = new Product { Name = "Sumo", Description = "Natural", Price = 2.0m };
            context.Products.Add(product);
            await context.SaveChangesAsync();

            var result = await controller.Delete(product.Id);

            Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(0, await context.Products.CountAsync());
        }

        [Fact]
        public async Task Edit_Post_UpdatesProductSuccessfully()
        {
            using var context = GetDatabaseContext();
            var controller = new ProductController(context);
            SetupTempData(controller);

            var product = new Product { Name = "Original", Description = "Desc", Price = 1m };
            context.Products.Add(product);
            await context.SaveChangesAsync();

            product.Name = "Editado";
            var model = new BarProductViewModel { Product = product };

            var result = await controller.Edit(product.Id, model);

            Assert.IsType<RedirectToActionResult>(result);
            var updated = await context.Products.FindAsync(product.Id);
            Assert.Equal("Editado", updated.Name);
        }
    }
}