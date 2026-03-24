using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using Projeto_SEGUES.Areas.Admin;
using Projeto_SEGUES.Areas.Inventory.ViewModels;
using Projeto_SEGUES.Models.Inventory;
using Projeto_SEGUES.Services;

namespace SeguesTests.Admin
{
    public class AdminInventoryManagementControllerTests
    {
        private readonly Mock<IInventoryService> _mockInventoryService;
        private readonly AdminInventoryManagementController _controller;

        public AdminInventoryManagementControllerTests()
        {
            _mockInventoryService = new Mock<IInventoryService>();
            //_controller = new AdminInventoryManagementController(_mockInventoryService.Object);

            var httpContext = new DefaultHttpContext();
            _controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
        }

        // Ensures Index populates categories and products for the view
        [Fact]
        public async Task Index_ReturnsView_AndPopulatesViewBag()
        {
            _mockInventoryService.Setup(s => s.GetAllCategoriesForDropdownAsync()).ReturnsAsync(new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>());
            _mockInventoryService.Setup(s => s.GetAllProductsAsync()).ReturnsAsync(new List<Product>());

            var result = await _controller.Index();

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.NotNull(_controller.ViewBag.Categories);
            Assert.NotNull(_controller.ViewBag.Products);
        }

        // Verifies that GetProducts returns the correct partial view
        [Fact]
        public async Task GetProducts_ReturnsPartialView_WithModel()
        {
            var products = new List<Product> { new Product { Id = 1, Name = "Test",Description = "descriptiontesst",Price = 1.50m, MinimumStock = 2,
                Stock = 50, Category = new ProductCategory { Name = "Bar", Description = "descripitiontets" } } };
            _mockInventoryService.Setup(s => s.GetAllProductsAsync()).ReturnsAsync(products);

            var result = await _controller.GetProducts();

            var partialResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("_ProductListPartial", partialResult.ViewName);
            Assert.Equal(products, partialResult.Model);
        }

        // Redirects to Index and sets error message on invalid model
        [Fact]
        public async Task Create_InvalidModel_RedirectsWithErrorMessage()
        {
            _controller.ModelState.AddModelError("Name", "Required");
            var model = new ProductViewModel
            {
                Name = "test product",
                Description = "tset edscription",
                CategoryId = 1,
                Price = 15.00m,
                Stock = 5,
                MinimumStock = 1
            };

            var result = await _controller.Create(model);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            var swalValue = _controller.TempData.Values.FirstOrDefault()?.ToString();
            Assert.Contains("Verifique os campos", swalValue);
        }

        // Returns NotFound when editing a product that does not exist
        [Fact]
        public async Task Edit_Get_InvalidId_ReturnsNotFound()
        {
            _mockInventoryService.Setup(s => s.GetProductByIdAsync(99)).ReturnsAsync((Product)null!);

            var result = await _controller.Edit(99);

            Assert.IsType<NotFoundResult>(result);
        }

        // Correctly maps Product entity to ProductViewModel for editing
        [Fact]
        public async Task Edit_Get_ValidId_ReturnsViewWithViewModel()
        {
            var product = new Product
            {
                Id = 1,
                Name = "Coffee",
                Description = "Very good for the sleep",
                MinimumStock = 2,
                Price = 12,
                Stock = 500,
                Category = new ProductCategory { Id = 1, Description = "Food", Name = "Eating things" }
            };
            _mockInventoryService.Setup(s => s.GetProductByIdAsync(1)).ReturnsAsync(product);

            var result = await _controller.Edit(1);

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<ProductViewModel>(viewResult.Model);
            Assert.Equal("Coffee", model.Name);
        }

        // Handles service failure during product deletion
        [Fact]
        public async Task Delete_ServiceFails_SetsErrorMessage()
        {
            _mockInventoryService.Setup(s => s.DeleteProductAsync(1))
                .ReturnsAsync(ServiceResult.Fail("Product in use"));

            var result = await _controller.Delete(1);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            var swalValue = _controller.TempData.Values.FirstOrDefault()?.ToString();
            Assert.Contains("Product in use", swalValue);
        }
    }
}