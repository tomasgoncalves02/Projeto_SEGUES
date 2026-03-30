using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using Projeto_SEGUES.Areas.Admin.Controllers;
using Projeto_SEGUES.Areas.Admin.ViewModels;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Services;
using Xunit;

namespace SeguesTests.UnitTests.Admin
{
    public class AdminCreateInternalAccountControllerUnitTests
    {
        private readonly Mock<IAdminService> _adminServiceMock;
        private readonly AdminCreateInternalAccountController _controller;
        private readonly Mock<ITempDataDictionary> _tempDataMock;

        public AdminCreateInternalAccountControllerUnitTests()
        {
            _adminServiceMock = new Mock<IAdminService>();
            _controller = new AdminCreateInternalAccountController(_adminServiceMock.Object);
            _tempDataMock = new Mock<ITempDataDictionary>();
            _controller.TempData = _tempDataMock.Object;
        }

        [Fact]
        public async Task Index_ReturnsView_WithPopulatedRoles()
        {
            var roles = new List<SelectListItem> { new() { Value = "Admin", Text = "Admin" } };
            _adminServiceMock.Setup(s => s.GetNonClientRolesForDropdownAsync()).ReturnsAsync(roles);

            var result = await _controller.Index();

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(roles, _controller.ViewBag.Roles);
        }

        [Fact]
        public async Task Create_ReturnsView_WhenModelStateIsInvalid()
        {
            _controller.ModelState.AddModelError("FirstName", "Required");
            var roles = new List<SelectListItem> { new() { Value = "Staff", Text = "Staff" } };
            _adminServiceMock.Setup(s => s.GetNonClientRolesForDropdownAsync()).ReturnsAsync(roles);

            var model = new CreateInternalUserViewModel
            {
                FirstName = "Pedro",
                LastName = "Jesus",
                Email = "pedro@test.pt",
                AccountType = "Admin",
                Gender = Gender.Male,
                BirthDate = DateTime.Now
            };

            var result = await _controller.Create(model);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Index", viewResult.ViewName);
            Assert.Equal(roles, _controller.ViewBag.Roles);
            _adminServiceMock.Verify(s => s.CreateInternalUserAsync(It.IsAny<CreateInternalUserViewModel>()), Times.Never);
        }

        [Fact]
        public async Task Create_RedirectsToIndex_WhenServiceSucceeds()
        {
            var model = new CreateInternalUserViewModel
            {
                FirstName = "Pedro",
                LastName = "Jesus",
                Email = "pedro@test.pt",
                AccountType = "Admin",
                Gender = Gender.Male,
                BirthDate = DateTime.Now.AddYears(-20)
            };

            _adminServiceMock.Setup(s => s.CreateInternalUserAsync(model))
                .ReturnsAsync(ServiceResult.Ok("Sucesso"));

            var result = await _controller.Create(model);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
        }

        [Fact]
        public async Task Create_ReturnsView_WhenServiceFails()
        {
            var model = new CreateInternalUserViewModel
            {
                FirstName = "Pedro",
                LastName = "Jesus",
                Email = "pedro@test.pt",
                AccountType = "Admin",
                Gender = Gender.Male,
                BirthDate = DateTime.Now.AddYears(-20)
            };

            var roles = new List<SelectListItem>();
            _adminServiceMock.Setup(s => s.CreateInternalUserAsync(model))
                .ReturnsAsync(ServiceResult.Fail("Erro ao criar"));
            _adminServiceMock.Setup(s => s.GetNonClientRolesForDropdownAsync()).ReturnsAsync(roles);

            var result = await _controller.Create(model);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Index", viewResult.ViewName);
            Assert.Equal(roles, _controller.ViewBag.Roles);
        }
    }
}