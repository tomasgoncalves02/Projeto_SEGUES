using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Projeto_SEGUES.Areas.User.Controllers;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;
using System.Security.Claims;
using Xunit;

namespace SeguesTests.Users
{
    public class UserControllerTests
    {
        private Mock<UserManager<AppUser>> GetMockUserManager() =>
            new Mock<UserManager<AppUser>>(new Mock<IUserStore<AppUser>>().Object, null, null, null, null, null, null, null, null);

        private (UserController, Mock<UserManager<AppUser>>, Mock<IAdminService>, AppUser) SetupController()
        {
            var mockUserMgr = GetMockUserManager();
            var mockAdminSvc = new Mock<IAdminService>();

            var controller = new UserController(mockAdminSvc.Object, mockUserMgr.Object);

            var user = new AppUser
            {
                Id = "u-user",
                FirstName = "Diogo",
                LastName = "Teste",
                UserCategory = new UserCategory { Name = "Cliente" },
                BirthDate = new DateTime(1995, 1, 1),
                Gender = Projeto_SEGUES.Models.Enums.Gender.Male
            };

            var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, user.Id) }, "TestAuth");
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };

            mockUserMgr.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
            mockUserMgr.Setup(m => m.UpdateAsync(It.IsAny<AppUser>())).ReturnsAsync(IdentityResult.Success);

            return (controller, mockUserMgr, mockAdminSvc, user);
        }

        [Fact]
        public async Task Index_ReturnsViewWithRoles()
        {
            var (controller, _, mockAdminSvc, _) = SetupController();

            var roles = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>();
            mockAdminSvc.Setup(s => s.GetAllRolesForDropdownAsync()).ReturnsAsync(roles);

            var result = await controller.Index();

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.NotNull(controller.ViewBag.Roles);
        }

        [Fact]
        public async Task UpdateType_ValidName_ReturnsOk()
        {
            var (controller, _, _, user) = SetupController();

            var result = await controller.UpdateType("name", "Joao");

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("Joao", user.FirstName);
        }

        [Fact]
        public async Task UpdateType_InvalidNameWithNumbers_ReturnsBadRequest()
        {
            var (controller, _, _, _) = SetupController();

            var result = await controller.UpdateType("name", "Joao123");

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task UpdateType_UnderageBirthDate_ReturnsBadRequest()
        {
            var (controller, _, _, _) = SetupController();
            var underageDate = DateTime.Now.AddYears(-15).ToString("yyyy-MM-dd");

            var result = await controller.UpdateType("birthDate", underageDate);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task UpdatePassword_Success_ReturnsOk()
        {
            var (controller, mockUserMgr, _, user) = SetupController();

            mockUserMgr.Setup(m => m.ChangePasswordAsync(user, "OldPass123@A", "NewPass123@B"))
                .ReturnsAsync(IdentityResult.Success);

            var result = await controller.UpdatePassword("OldPass123@A", "NewPass123@B");

            var okResult = Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task UpdatePassword_Failure_ReturnsBadRequest()
        {
            var (controller, mockUserMgr, _, user) = SetupController();

            mockUserMgr.Setup(m => m.ChangePasswordAsync(user, "OldPass123@A", "NewPass123@B"))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Error" }));

            var result = await controller.UpdatePassword("OldPass123@A", "NewPass123@B");

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public void GetGenders_ReturnsJsonList()
        {
            var (controller, _, _, _) = SetupController();

            var result = controller.GetGenders();

            Assert.IsType<JsonResult>(result);
        }
    }
}