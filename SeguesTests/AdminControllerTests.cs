using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using Projeto_SEGUES.Areas.Admin;
using Projeto_SEGUES.Areas.Admin.ViewModels;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;
using SeguesTests.Helpers;
using Xunit;

namespace SeguesTests
{
    public class AdminControllerTests
    {
        private readonly Mock<UserManager<AppUser>> _mockUserManager;
        private readonly Mock<RoleManager<Role>> _mockRoleManager;
        private readonly Mock<IAdminService> _mockAdminService;
        private readonly AppDbContext _context;

        private readonly AdminCreateInternalAccountController _createInternalAccountController;
        private readonly AdminUserManagementController _userManagementController;

        public AdminControllerTests()
        {
            var usersList = new List<AppUser>();
            _mockUserManager = MockHelper.MockUserManager(usersList);
            _mockRoleManager = MockHelper.MockRoleManager<Role>();
            _mockAdminService = new Mock<IAdminService>();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDb_" + System.Guid.NewGuid())
                .Options;
            _context = new AppDbContext(options);

            _createInternalAccountController = new AdminCreateInternalAccountController(_mockAdminService.Object);
            _userManagementController = new AdminUserManagementController(_mockUserManager.Object, _mockAdminService.Object);

            SetupControllerContext(_createInternalAccountController);
            SetupControllerContext(_userManagementController);
        }

        private void SetupControllerContext(Controller controller)
        {
            var httpContext = new DefaultHttpContext();
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
        }

        [Fact]
        public async Task CreateInternalAccount_DeveCriarUser_E_RedirecionarParaIndex()
        {
            var model = new CreateInternalUserViewModel
            {
                FirstName = "Maria",
                LastName = "Silva",
                Email = "maria@test.com",
                Gender = Gender.Female,
                BirthDate = DateTime.Now.AddYears(-20),
                AccountType = "Admin"
            };

            _mockAdminService.Setup(x => x.CreateInternalUserAsync(model))
                .ReturnsAsync(IdentityResult.Success);

            var result = await _createInternalAccountController.Create(model);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
        }

        [Fact]
        public async Task Edit_DeveAtualizarUser_E_RedirecionarParaIndex()
        {
            var userId = "user-123";
            var user = new AppUser
            {
                Id = userId,
                Email = "velho@test.com",
                FirstName = "Antigo",
                LastName = "Nome",
                Gender = Gender.Male,
                BirthDate = DateTime.Now.AddYears(-30),
                UserCategory = new UserCategory { Name = "Externo" }
            };

            _mockUserManager.Setup(u => u.FindByIdAsync(userId)).ReturnsAsync(user);
            _mockUserManager.Setup(u => u.UpdateAsync(It.IsAny<AppUser>())).ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(u => u.GetRolesAsync(It.IsAny<AppUser>())).ReturnsAsync(new List<string> { "Client" });
            _mockUserManager.Setup(u => u.RemoveFromRolesAsync(It.IsAny<AppUser>(), It.IsAny<IEnumerable<string>>())).ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(u => u.AddToRoleAsync(It.IsAny<AppUser>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);

            var model = new EditUserViewModel
            {
                Id = userId,
                FirstName = "Novo",
                LastName = "Nome",
                Email = "novo@test.com",
                Category = "Externo",
                Role = "Admin",
                Gender = Gender.Male,
                BirthDate = DateTime.Now.AddYears(-30),
                Balance = 0
            };

            var result = await _userManagementController.Edit(model);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
        }

        [Fact]
        public async Task Deactivate_DeveMudarStatusParaInactive_E_Redirecionar()
        {
            var userId = "user-deactivate";
            var user = new AppUser
            {
                Id = userId,
                UserName = "outro@test.com",
                FirstName = "User",
                LastName = "Teste",
                Email = "outro@test.com",
                Gender = Gender.Other,
                BirthDate = DateTime.Now.AddYears(-25),
                UserCategory = new UserCategory { Name = "Externo" }
            };

            _mockUserManager.Setup(u => u.FindByIdAsync(userId)).ReturnsAsync(user);
            _mockUserManager.Setup(u => u.UpdateAsync(It.IsAny<AppUser>())).ReturnsAsync(IdentityResult.Success);

            var result = await _userManagementController.Deactivate(userId);

            Assert.Equal(UserStatus.Inactive, user.Status);
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
        }
    }
}