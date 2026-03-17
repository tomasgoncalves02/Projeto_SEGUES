using Microsoft.AspNetCore.Identity;
using Moq;
using Projeto_SEGUES.Areas.User.Controllers;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;

namespace SeguesTests.User
{
    public class UserControllerTests
    {
        private readonly Mock<IAdminService>? _mockAdminService;
        private readonly Mock<UserManager<AppUser>>? _mockUserManager;
        private readonly UserController? _controller;

        /*
        public UserControllerTests()
        {
            _mockAdminService = new Mock<IAdminService>();
            var store = new Mock<IUserStore<AppUser>>();
            _mockUserManager = new Mock<UserManager<AppUser>>(store.Object, null, null, null, null, null, null, null, null);

            _controller = new UserController(_mockAdminService.Object, _mockUserManager.Object);

            var httpContext = new DefaultHttpContext();
            _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        }

        // Helper method to create a valid user instance for testing purposes
        private AppUser CreatePedroUser() => new()
        {
            Id = "pedro-77",
            FirstName = "Pedro",
            LastName = "Profile",
            Email = "pedro@segues.pt",
            BirthDate = DateTime.Now.AddYears(-20),
            Gender = Gender.Male,
            UserCategory = new UserCategory
            {
                Id = 1,
                Name = "Estudante"
                
            }
        };

        // Confirms that the profile index page loads correctly with the required dropdown roles
        [Fact]
        public async Task Index_ReturnsView_WithRolesInViewBag()
        {
            // Fix CS1929: Change List<Role> to List<SelectListItem>
            _mockAdminService.Setup(s => s.GetAllRolesForDropdownAsync())
                .ReturnsAsync(new List<SelectListItem>
                {
            new SelectListItem { Value = "1", Text = "Admin" },
            new SelectListItem { Value = "2", Text = "Employee" }
                });

            var result = await _controller.Index();

            Assert.IsType<ViewResult>(result);
            Assert.NotNull(_controller.ViewBag.Roles);
        }

        // Verifies that the name update fails if the value contains symbols or numbers
        [Fact]
        public async Task UpdateType_InvalidName_ReturnsBadRequest()
        {
            var user = CreatePedroUser();
            _mockUserManager.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);

            var result = await _controller.UpdateType("name", "Pedro123");

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("não pode conter números", badRequest.Value?.ToString());
        }

        // Ensures that the system strictly enforces the minimum age of 18 for user accounts
        [Fact]
        public async Task UpdateType_BirthDateTooYoung_ReturnsBadRequest()
        {
            var user = CreatePedroUser();
            _mockUserManager.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
            var underageDate = DateTime.Now.AddYears(-17).ToString("yyyy-MM-dd");

            var result = await _controller.UpdateType("birthDate", underageDate);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("pelo menos 18 anos", badRequest.Value?.ToString());
        }

        // Validates that an email update follows the correct format and updates the username accordingly
        [Fact]
        public async Task UpdateType_ValidEmail_UpdatesSuccessfully()
        {
            var user = CreatePedroUser();
            _mockUserManager.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
            _mockUserManager.Setup(u => u.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

            var result = await _controller.UpdateType("email", "novo@segues.pt");

            Assert.IsType<OkObjectResult>(result);
            Assert.Equal("novo@segues.pt", user.Email);
            Assert.Equal("novo@segues.pt", user.UserName);
        }

        // Confirms that password updates are rejected if the new password does not meet complexity requirements
        [Fact]
        public async Task UpdatePassword_WeakPassword_ReturnsBadRequest()
        {
            var user = CreatePedroUser();
            _mockUserManager.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);

            var result = await _controller.UpdatePassword("OldPass123!", "weak");

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("mínimo 12 caracteres", badRequest.Value?.ToString());
        }

        // Verifies that a successful password change is processed when all security criteria are met
        [Fact]
        public async Task UpdatePassword_Success_ReturnsOk()
        {
            var user = CreatePedroUser();
            _mockUserManager.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
            _mockUserManager.Setup(u => u.ChangePasswordAsync(user, "OldPass123!", "NewStrongPass123!"))
                .ReturnsAsync(IdentityResult.Success);

            var result = await _controller.UpdatePassword("OldPass123!", "NewStrongPass123!");

            Assert.IsType<OkObjectResult>(result);
        }

        // Ensures the gender list is correctly retrieved as a JSON result for dynamic profile forms
        [Fact]
        public void GetGenders_ReturnsJsonList()
        {
            var result = _controller.GetGenders();

            var jsonResult = Assert.IsType<JsonResult>(result);
            var list = Assert.IsAssignableFrom<IEnumerable<object>>(jsonResult.Value);
            Assert.NotEmpty(list);
        }*/
    }
}