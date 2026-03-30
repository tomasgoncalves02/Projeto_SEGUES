using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using Projeto_SEGUES.Areas.Admin;
using Projeto_SEGUES.Areas.Admin.Controllers;
using Projeto_SEGUES.Areas.Admin.ViewModels;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Services;

namespace SeguesTests.Admin
{
    public class AdminCreateInternalAccountControllerTests
    {
        private readonly Mock<IAdminService> _mockAdminService;
        private readonly AdminCreateInternalAccountController _controller;

        public AdminCreateInternalAccountControllerTests()
        {
            _mockAdminService = new Mock<IAdminService>();
           // _ticketController = new AdminCreateInternalAccountController(_mockAdminService.Object);

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


        // Tests if Index view returns roles for the dropdown
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


        // Ensures invalid model returns view with roles repopulated
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


        // Redirects to Index after successful account creation
        [Fact]
        public async Task Create_ValidModel_Success_RedirectsToIndex()
        {
            var model = CreateValidModel();
            _mockAdminService.Setup(s => s.CreateInternalUserAsync(model))
                .ReturnsAsync(ServiceResult.Ok());

            var result = await _controller.Create(model);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
        }


        // Handles service failures like duplicate emails correctly
        [Fact]
        public async Task Create_ServiceFails_ReturnsViewWithErrorsAndPopulatesRoles()
        {
            var model = CreateValidModel();

            _mockAdminService.Setup(s => s.CreateInternalUserAsync(model))
                .ReturnsAsync(ServiceResult.Fail("O e-mail já está em uso."));

            var roles = new List<SelectListItem> { new SelectListItem { Value = "Admin", Text = "Admin" } };
            _mockAdminService.Setup(s => s.GetNonClientRolesForDropdownAsync())
                .ReturnsAsync(roles);

            var result = await _controller.Create(model);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Index", viewResult.ViewName);
            Assert.False(_controller.ModelState.IsValid);
            Assert.Equal(roles, _controller.ViewBag.Roles);
        }


        // Handles critical system exceptions like email failures
        [Fact]
        public async Task Create_ServiceThrowsException_ReturnsViewWithErrorMessage()
        {
            var model = CreateValidModel();
            _mockAdminService.Setup(s => s.CreateInternalUserAsync(model))
                .ThrowsAsync(new System.Exception("Erro de SMTP"));

            var roles = new List<SelectListItem> { new SelectListItem { Value = "Admin", Text = "Admin" } };
            _mockAdminService.Setup(s => s.GetNonClientRolesForDropdownAsync()).ReturnsAsync(roles);

            var result = await _controller.Create(model);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Index", viewResult.ViewName);

            Assert.NotEmpty(_controller.TempData);
            var swalValue = _controller.TempData.Values.FirstOrDefault()?.ToString();
            Assert.NotNull(swalValue);
            Assert.Contains("Falha na", swalValue);
        }


        // Verifies that a success notification is set
        [Fact]
        public async Task Create_ValidModel_Success_SetsTempDataMessage()
        {
            var model = CreateValidModel();
            _mockAdminService.Setup(s => s.CreateInternalUserAsync(model))
                .ReturnsAsync(ServiceResult.Ok());

            var result = await _controller.Create(model);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);

            Assert.NotEmpty(_controller.TempData);
            var swalValue = _controller.TempData.Values.FirstOrDefault()?.ToString();
            Assert.Contains($"Conta criada para {model.FirstName}", swalValue);
        }


        // Ensures all service errors are added to ModelState
        [Fact]
        public async Task Create_ServiceFailsWithMultipleErrors_AddsAllToModelState()
        {
            var model = CreateValidModel();
            var errors = "Email inválido; Senha fraca";

            _mockAdminService.Setup(s => s.CreateInternalUserAsync(model))
                .ReturnsAsync(ServiceResult.Fail(errors));

            _mockAdminService.Setup(s => s.GetNonClientRolesForDropdownAsync())
                .ReturnsAsync(new List<SelectListItem>());

            var result = await _controller.Create(model);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(2, _controller.ModelState.ErrorCount);
        }


        // Ensures the explicit Index view is returned on failure
        [Fact]
        public async Task Create_ServiceFails_ReturnsExplicitIndexView()
        {

            var model = CreateValidModel();
            _mockAdminService.Setup(s => s.CreateInternalUserAsync(model))
                .ReturnsAsync(ServiceResult.Fail("Erro"));

            var result = await _controller.Create(model);

            var viewResult = Assert.IsType<ViewResult>(result);

            Assert.Equal("Index", viewResult.ViewName);
        }


        // Ensures warning redundancy in case of critical failure
        [Fact]
        public async Task Create_ExceptionPath_SetsBothModelStateAndTempData()
        {

            var model = CreateValidModel();
            _mockAdminService.Setup(s => s.CreateInternalUserAsync(model))
                .ThrowsAsync(new System.Exception());

            await _controller.Create(model);

            Assert.False(_controller.ModelState.IsValid);
            Assert.NotEmpty(_controller.TempData);
        }

    }
}