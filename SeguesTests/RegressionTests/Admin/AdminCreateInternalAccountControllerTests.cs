using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using Projeto_SEGUES.Areas.Admin.Controllers;
using Projeto_SEGUES.Areas.Admin.ViewModels;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Services;
using Xunit;

namespace SeguesTests.RegressionTests.Admin
{
    public class AdminCreateInternalAccountControllerTests
    {
        private readonly Mock<IAdminService> _adminServiceMock;
        private readonly AdminCreateInternalAccountController _controller;
        private readonly Mock<ITempDataDictionary> _tempDataMock;

        public AdminCreateInternalAccountControllerTests()
        {
            _adminServiceMock = new Mock<IAdminService>();
            _controller = new AdminCreateInternalAccountController(_adminServiceMock.Object);
            _tempDataMock = new Mock<ITempDataDictionary>();
            _controller.TempData = _tempDataMock.Object;
        }

        [Fact]
        public async Task Index_ReturnsViewWithRolesInViewBag()
        {
            var roles = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>
            {
                new() { Value = "Admin", Text = "Administrador" }
            };
            _adminServiceMock.Setup(s => s.GetNonClientRolesForDropdownAsync()).ReturnsAsync(roles);

            var result = await _controller.Index();

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(roles, _controller.ViewBag.Roles);
        }

        [Fact]
        public async Task Create_ReturnsView_WhenModelStateIsInvalid()
        {
            _controller.ModelState.AddModelError("FirstName", "Required");
            var model = new CreateInternalUserViewModel
            {
                FirstName = "",
                LastName = "",
                Email = "",
                AccountType = "Admin",
                Gender = Gender.Other,
                BirthDate = DateTime.Now
            };
            var roles = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>();
            _adminServiceMock.Setup(s => s.GetNonClientRolesForDropdownAsync()).ReturnsAsync(roles);

            var result = await _controller.Create(model);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Index", viewResult.ViewName);
            Assert.Equal(roles, _controller.ViewBag.Roles);
            _adminServiceMock.Verify(s => s.CreateInternalUserAsync(It.IsAny<CreateInternalUserViewModel>()), Times.Never);
        }

        [Fact]
        public async Task Create_RedirectsToIndex_OnServiceSuccess()
        {
            var model = new CreateInternalUserViewModel
            {
                Email = "pedro@segues.pt",
                FirstName = "Pedro",
                LastName = "Regressao",
                AccountType = "Admin",
                Gender = Gender.Male,
                BirthDate = DateTime.Now.AddYears(-25)
            };

            _adminServiceMock.Setup(s => s.CreateInternalUserAsync(model))
                .ReturnsAsync(ServiceResult.Ok("Sucesso"));

            var result = await _controller.Create(model);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
        }
    }
}