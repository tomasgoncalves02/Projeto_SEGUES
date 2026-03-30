using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging; // ✅ Necessário para o ILogger
using Moq;
using Projeto_SEGUES.Areas.Admin.Controllers;
using Projeto_SEGUES.Areas.Admin.ViewModels;
using Projeto_SEGUES.Areas.Inventory.ViewModels;
using Projeto_SEGUES.Models.Inventory;
using Projeto_SEGUES.Services;
using SeguesTests.Helpers;
using Xunit;

namespace SeguesTests.IntegrationTests.Admin
{
    public class AdminInventoryIntegrationTests
    {
        [Fact]
        public async Task Edit_Post_ValidUpdate_PersistsChangesInDb()
        {
            var (context, _, _) = MockHelper.GetIdentitySetup();

            var service = new InventoryService(context, Mock.Of<ILogger<InventoryService>>());
            var controller = new AdminInventoryManagementController(service);
            controller.TempData = new Mock<ITempDataDictionary>().Object;

            var cat = new ProductCategory { Name = "Bar", Description = "D" };
            context.ProductCategory.Add(cat);
            var product = new Product
            {
                Name = "Pedro Drink",
                Description = "D",
                Category = cat,
                Price = 2,
                Stock = 10,
                MinimumStock = 1,
                IsActive = true
            };
            context.Product.Add(product);
            await context.SaveChangesAsync();

            var model = new CreateProductViewModel
            {
                Id = product.Id,
                Name = "Pedro Updated Drink",
                Description = "D",
                CategoryId = cat.Id,
                Price = 5,
                Stock = 20,
                MinimumStock = 1,
                IsActive = true
            };

            var result = await controller.Edit(model);

            Assert.IsType<RedirectToActionResult>(result);
            var updated = await context.Product.FindAsync(product.Id);
            Assert.Equal("Pedro Updated Drink", updated?.Name);
        }
    }
}