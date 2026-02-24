using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering; 
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using Projeto_SEGUES.Areas.Admin;
using Projeto_SEGUES.Areas.Admin.ViewModels;
using Projeto_SEGUES.Models.Enums; 
using Projeto_SEGUES.Services;
using Xunit;

namespace SeguesTests.Admin
{
    public class AdminCreateInternalAccountControllerTests
    {
        private readonly Mock<IAdminService> _mockAdminService;
        private readonly AdminCreateInternalAccountController _controller;

        public AdminCreateInternalAccountControllerTests()
        {
            _mockAdminService = new Mock<IAdminService>();
            _controller = new AdminCreateInternalAccountController(_mockAdminService.Object);

            var httpContext = new DefaultHttpContext();
            _controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
        }

        private CreateInternalUserViewModel CreateValidModel() => new()
        {
            FirstName = "Joao",
            LastName = "Silva",
            Email = "joao@test.com",
            Gender = Gender.Male, 
            BirthDate = DateTime.Now.AddYears(-20),
            AccountType = "Admin"
        };

        [Fact]
        public async Task Index_ReturnsView_AndPopulatesRoles()
        {
            var roles = new List<SelectListItem>
            {
                new SelectListItem { Value = "Admin", Text = "Admin" }
            };

            _mockAdminService.Setup(s => s.GetNonClientRolesForDropdownAsync())
                .ReturnsAsync(roles);

            var result = await _controller.Index();

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(roles, _controller.ViewBag.Roles);
        }

        [Fact]
        public async Task Create_InvalidModel_ReturnsViewWithRoles()
        {
            _controller.ModelState.AddModelError("FirstName", "Required");

            var roles = new List<SelectListItem> { new SelectListItem { Value = "Admin", Text = "Admin" } };
            _mockAdminService.Setup(s => s.GetNonClientRolesForDropdownAsync()).ReturnsAsync(roles);

            var model = CreateValidModel();

            var result = await _controller.Create(model);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(roles, _controller.ViewBag.Roles);
        }

        [Fact]
        public async Task Create_ValidModel_Success_RedirectsToIndex()
        {
            var model = CreateValidModel();
            _mockAdminService.Setup(s => s.CreateInternalUserAsync(model))
                .ReturnsAsync(IdentityResult.Success);

            var result = await _controller.Create(model);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
        }

        [Fact]
        public async Task Create_ServiceFails_ReturnsViewWithErrorsAndPopulatesRoles()
        {
            var model = CreateValidModel();
            var identityError = new IdentityError { Description = "O e-mail já está em uso." };

            _mockAdminService.Setup(s => s.CreateInternalUserAsync(model))
                .ReturnsAsync(IdentityResult.Failed(identityError));

            var roles = new List<SelectListItem> { new SelectListItem { Value = "Admin", Text = "Admin" } };
            _mockAdminService.Setup(s => s.GetNonClientRolesForDropdownAsync())
                .ReturnsAsync(roles);

            var result = await _controller.Create(model);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Index", viewResult.ViewName); 
            Assert.False(_controller.ModelState.IsValid); 
            Assert.Equal(roles, _controller.ViewBag.Roles); 
        }

    }
}