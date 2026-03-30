using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using Projeto_SEGUES.Areas.Admin.Controllers;
using Projeto_SEGUES.Areas.Admin.ViewModels;
using Projeto_SEGUES.Areas.Inventory.ViewModels;
using Projeto_SEGUES.Models.Inventory;
using Projeto_SEGUES.Services;
using Xunit;

namespace SeguesTests.UnitTests.Admin
{
    public class AdminInventoryManagementUnitTests
    {
        private readonly Mock<IInventoryService> _inventoryServiceMock;
        private readonly AdminInventoryManagementController _controller;
        private readonly Mock<ITempDataDictionary> _tempDataMock;

        public AdminInventoryManagementUnitTests()
        {
            _inventoryServiceMock = new Mock<IInventoryService>();
            _controller = new AdminInventoryManagementController(_inventoryServiceMock.Object);
            _tempDataMock = new Mock<ITempDataDictionary>();
            _controller.TempData = _tempDataMock.Object;
        }

        [Fact]
        public async Task Index_ReturnsView_WithViewModel()
        {
            var roles = new List<SelectListItem>();
            var products = new List<Product>
            {
                new Product
                {
                    Id = 1, Name = "Pedro's Coffee", Description = "D", Price = 1,
                    Stock = 10, MinimumStock = 1, IsActive = true,
                    Category = new ProductCategory { Id = 1, Name = "C", Description = "D" }
                }
            };

            _inventoryServiceMock.Setup(s => s.GetAllCategoriesForDropdownAsync()).ReturnsAsync(roles);
            _inventoryServiceMock.Setup(s => s.GetAllProductsAsync()).ReturnsAsync(products);

            var result = await _controller.Index();

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<InventoryManagementViewModel>(viewResult.Model);
            Assert.Single(model.Products);
        }

        [Fact]
        public async Task Create_InvalidModel_RedirectsToIndex()
        {
            _controller.ModelState.AddModelError("NewProduct.Name", "Required");
            var model = new CreateProductViewModel
            {
                Name = "Pedro Product",
                Description = "D",
                CategoryId = 1,
                Price = 10,
                Stock = 5,
                MinimumStock = 1
            };

            var result = await _controller.Create(model);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            _tempDataMock.VerifySet(t => t[It.Is<string>(k => k.Contains("Swal"))] = It.IsAny<object>(), Times.Once);
        }
    }
}