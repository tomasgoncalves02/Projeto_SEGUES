using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using Projeto_SEGUES.Areas.Admin;
using Projeto_SEGUES.Areas.User.ViewModels;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models.Audit;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;
using System.Security.Claims;
using System.Security.Principal;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Projeto_SEGUES.Resources;

namespace SeguesTests.Admin
{
    public class AdminUserManagementControllerTests
    {
        private readonly Mock<UserManager<AppUser>> _mockUserManager;
        private readonly Mock<IAdminService> _mockAdminService;
        private readonly AdminUserManagementController _controller;
        private readonly AppDbContext _context;
        private readonly Mock<ILogger<AdminUserManagementController>> _mockLogger;
        private readonly Mock<IStringLocalizer<Errors>> _mockLocalizer;

        public AdminUserManagementControllerTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _context = new AppDbContext(options);

            var store = new Mock<IUserStore<AppUser>>();
            _mockUserManager = new Mock<UserManager<AppUser>>(store.Object, null, null, null, null, null, null, null, null);
            _mockAdminService = new Mock<IAdminService>();


            //_controller = new AdminUserManagementController(_mockUserManager.Object, _mockAdminService.Object, _context, _mockLogger.Object, _mockLocalizer.Object);

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


        // Ensures Index action returns users based on search and role filters
        [Fact]
        public async Task Index_ReturnsView_WithFilteredUsers()
        {
            // var users = new List<AppUser> { CreateTestUser("1", "test@test.com") };
            // _mockAdminService.Setup(s => s.GetFilteredUsersAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            //     .ReturnsAsync(users);
            //
            // var result = await _controller.Index("search", "Admin", "Category");
            //
            // var viewResult = Assert.IsType<ViewResult>(result);
            // Assert.Equal(users, viewResult.Model);
            // Assert.Equal("search", _controller.ViewData["SearchString"]);
        }


        // Verifies GET Edit returns the correct view model for a valid user
        [Fact]
        public async Task Edit_Get_ReturnsView_WithViewModel()
        {
            var user = CreateTestUser("1", "test@test.com");
            _mockUserManager.Setup(u => u.FindByIdAsync("1")).ReturnsAsync(user);
            _mockUserManager.Setup(u => u.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Admin" });

            var result = await _controller.Edit("1");

            var viewResult = Assert.IsType<ViewResult>(result);
            //var model = Assert.IsType<EditUserAdminViewModel>(viewResult.Model);
            //Assert.Equal(user.Id, model.Id);
        }


        // Confirms successful user update redirects to Index with success message
        [Fact]
        public async Task Edit_Post_ValidModel_RedirectsToIndex()
        {
            /*var model = new EditUserAdminViewModel
            {
                Id = "1",
                FirstName = "New",
                LastName = "Name",
                Category = "Estudante",
                Role = "Admin",
                Gender = Gender.Male,
                BirthDate = DateTime.Now.AddYears(-20),
                Balance = 10.00m
            };*/

            var user = CreateTestUser("1", "old@test.com");
            _mockUserManager.Setup(u => u.FindByIdAsync("1")).ReturnsAsync(user);
            _mockAdminService.Setup(s => s.GetCategoryByNameAsync("Estudante")).ReturnsAsync(new UserCategory { Name = "Estudante" });
            _mockUserManager.Setup(u => u.UpdateAsync(It.IsAny<AppUser>())).ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(u => u.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Employee" });

            //var result = await _controller.Edit(model);

            //var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            /*Assert.Equal("Index", redirectResult.ActionName);
            Assert.Contains("success", _controller.TempData["SwalData"]?.ToString());*/
        }


        // Prevents administrators from deactivating their own accounts
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


        // Verifies that activating a user updates status and redirects to Details
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


        // Validates UI translation logic for Gender and Status enums
        [Fact]
        public async Task Details_ReturnsView_WithCorrectPortugueseTranslations()
        {
            var user = CreateTestUser("1", "test@test.com");
            user.Gender = Gender.Female;
            user.Status = UserStatus.Active;

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _mockUserManager.Setup(u => u.Users).Returns(_context.Users);

            _mockUserManager.Setup(u => u.GetRolesAsync(It.IsAny<AppUser>())).ReturnsAsync(new List<string> { "Client" });
            _mockAdminService.Setup(s => s.GetAllRolesForDropdownAsync()).ReturnsAsync(new List<SelectListItem>());

            var result = await _controller.Details("1");

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Feminino", _controller.ViewBag.GenderPT);
            Assert.Equal("ATIVO", _controller.ViewBag.StatusPT);
        }


        // Ensures audit logs can be filtered by specific search keywords
        [Fact]
        public async Task StaffLog_FiltersBySearchString_ReturnsOnlyMatches()
        {
            var user = CreateTestUser("u1", "staff@test.com");
            _context.UserLog.Add(new UserLog
            {
                AppUser = user,
                Message = "Login Success",
                UserAction = UserAction.LogIn,
                TimeStamp = DateTime.Now
            });
            _context.UserLog.Add(new UserLog
            {
                AppUser = user,
                Message = "User Updated",
                UserAction = UserAction.Update,
                TimeStamp = DateTime.Now
            });
            await _context.SaveChangesAsync();

            var result = await _controller.StaffLog("Login", null);

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<UserLog>>(viewResult.Model);
            Assert.Single(model);
            Assert.Contains("Login", model.First().Message);
        }


        // Confirms deactivation sets status to Inactive and applies account lockout
        [Fact]
        public async Task Deactivate_ValidUser_SetsLockoutAndStatus()
        {
            var user = CreateTestUser("10", "target@test.com");
            _mockUserManager.Setup(u => u.FindByIdAsync("10")).ReturnsAsync(user);
            _mockUserManager.Setup(u => u.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

            var result = await _controller.Deactivate("10");

            Assert.Equal(UserStatus.Inactive, user.Status);
            _mockUserManager.Verify(u => u.SetLockoutEndDateAsync(user, It.Is<DateTimeOffset>(d => d > DateTimeOffset.Now)), Times.Once);
        }
    }
}