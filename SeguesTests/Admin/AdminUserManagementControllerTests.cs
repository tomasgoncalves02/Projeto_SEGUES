using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using Projeto_SEGUES.Areas.Admin;
using Projeto_SEGUES.Areas.Admin.ViewModels;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;
using System.Security.Claims;
using System.Security.Principal;
using Xunit;

namespace SeguesTests.Admin
{
    public class AdminUserManagementControllerTests
    {
        private readonly Mock<UserManager<AppUser>> _mockUserManager;
        private readonly Mock<IAdminService> _mockAdminService;
        private readonly AdminUserManagementController _controller;

        public AdminUserManagementControllerTests()
        {
            var store = new Mock<IUserStore<AppUser>>();
            _mockUserManager = new Mock<UserManager<AppUser>>(store.Object, null, null, null, null, null, null, null, null);
            _mockAdminService = new Mock<IAdminService>();

            _controller = new AdminUserManagementController(_mockUserManager.Object, _mockAdminService.Object);

            var httpContext = new DefaultHttpContext();
            _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            _controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
        }

        private AppUser CreateTestUser(string id, string email) => new()
        {
            Id = id,
            Email = email,
            UserName = email,
            FirstName = "Teste",
            LastName = "User",
            UserCategory = new UserCategory { Name = "Estudante" },
            BirthDate = DateTime.Now.AddYears(-20),
            Gender = Gender.Male,
            Status = UserStatus.Active
        };

        [Fact]
        public async Task Index_ReturnsView_WithFilteredUsers()
        {
            var users = new List<AppUser> { CreateTestUser("1", "test@test.com") };
            _mockAdminService.Setup(s => s.GetFilteredUsersAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(users);

            var result = await _controller.Index("search", "Admin", "Category");

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(users, viewResult.Model);
            Assert.Equal("search", _controller.ViewData["SearchString"]);
        }

        [Fact]
        public async Task Edit_Get_ReturnsView_WithViewModel()
        {
            var user = CreateTestUser("1", "test@test.com");
            _mockUserManager.Setup(u => u.FindByIdAsync("1")).ReturnsAsync(user);
            _mockUserManager.Setup(u => u.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Admin" });

            var result = await _controller.Edit("1");

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<EditUserViewModel>(viewResult.Model);
            Assert.Equal(user.Email, model.Email);
        }

        [Fact]
        public async Task Edit_Post_ValidModel_RedirectsToIndex()
        {
            var model = new EditUserViewModel
            {
                Id = "1",
                Email = "new@test.com",
                FirstName = "New",
                LastName = "Name",
                Category = "Estudante",
                Role = "Admin",
                Gender = Gender.Male,
                BirthDate = DateTime.Now.AddYears(-20),
                Balance = 10.00m 
            };

            var user = CreateTestUser("1", "old@test.com");
            _mockUserManager.Setup(u => u.FindByIdAsync("1")).ReturnsAsync(user);
            _mockAdminService.Setup(s => s.GetCategoryByNameAsync("Estudante")).ReturnsAsync(new UserCategory { Name = "Estudante" });
            _mockUserManager.Setup(u => u.UpdateAsync(It.IsAny<AppUser>())).ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(u => u.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Employee" });

            var result = await _controller.Edit(model);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Contains("success", _controller.TempData["SwalData"]?.ToString());
        }

        [Fact]
        public async Task Deactivate_SelfDeactivation_ReturnsError()
        {
            var user = CreateTestUser("1", "admin@test.com");
            _mockUserManager.Setup(u => u.FindByIdAsync("1")).ReturnsAsync(user);

            var identity = new GenericIdentity("admin@test.com");
            _controller.HttpContext.User = new ClaimsPrincipal(identity);

            var result = await _controller.Deactivate("1");

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Contains("error", _controller.TempData["SwalData"]?.ToString());
        }

        [Fact]
        public async Task Activate_ValidUser_RedirectsToDetails()
        {
            var user = CreateTestUser("2", "user@test.com");
            user.Status = UserStatus.Inactive;
            _mockUserManager.Setup(u => u.FindByIdAsync("2")).ReturnsAsync(user);
            _mockUserManager.Setup(u => u.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

            var result = await _controller.Activate("2");

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectResult.ActionName);
            Assert.Equal(UserStatus.Active, user.Status);
        }
    }
}