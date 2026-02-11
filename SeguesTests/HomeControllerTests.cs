using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Projeto_SEGUES.Controllers;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.User;
using SeguesTests.Helpers;

namespace SeguesTests
{
    public class HomeControllerTests
    {
        private readonly Mock<ILogger<HomeController>> _mockLogger;
        private readonly Mock<UserManager<AppUser>> _mockUserManager;

        public HomeControllerTests()
        {
            _mockLogger = new Mock<ILogger<HomeController>>();

            // Usamos o Helper para criar o Mock complexo do UserManager
            var usersList = new List<AppUser>();
            _mockUserManager = MockHelper.MockUserManager(usersList);
        }

        // Helper para configurar o Controller com contexto de utilizador
        private HomeController GetControllerWithUser(AppUser appUser = null, bool isAuthenticated = true)
        {
            var controller = new HomeController(_mockLogger.Object, _mockUserManager.Object);

            var claims = new List<Claim>();
            if (appUser != null)
            {
                claims.Add(new Claim(ClaimTypes.NameIdentifier, appUser.Id ?? "1"));
                claims.Add(new Claim(ClaimTypes.Name, appUser.UserName ?? "test"));
            }

            var identity = new ClaimsIdentity(claims, isAuthenticated ? "TestAuthType" : null);
            var claimsPrincipal = new ClaimsPrincipal(identity);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            return controller;
        }

        [Fact]
        public async Task Index_RedirecionaParaLogin_SeNaoAutenticado()
        {
            // ARRANGE
            var controller = GetControllerWithUser(isAuthenticated: false);

            // ACT
            var result = await controller.Index();

            // ASSERT
            var redirectResult = Assert.IsType<RedirectToPageResult>(result);
            Assert.Equal("/Account/Login", redirectResult.PageName);
        }

        [Fact]
        public async Task Index_RetornaViewComDados_SeAutenticado()
        {
            // ARRANGE
            var user = new AppUser
            {
                Id = "123",
                UserName = "joao@teste.com",
                FirstName = "João",
                LastName = "Silva",
                Gender = Gender.Male,
                Balance = 15.50m,
                UserCategory = new UserCategory { Name = "Estudante" },
                BirthDate =  DateTime.Now.AddYears(-30),
            };

            // Configurar o Mock para devolver este user quando pedido
            _mockUserManager.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(user);
            _mockUserManager.Setup(u => u.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "Estudante" });
            _mockUserManager.Setup(u => u.GetUserNameAsync(user))
                .ReturnsAsync(user.UserName);

            var controller = GetControllerWithUser(user, isAuthenticated: true);

            // ACT
            var result = await controller.Index();

            // ASSERT
            var viewResult = Assert.IsType<ViewResult>(result);

            // Verificar se a ViewBag foi preenchida corretamente
            Assert.Equal(15.50m, viewResult.ViewData["UserBalance"]);
            Assert.Equal("João", viewResult.ViewData["FirstName"]);
            Assert.Equal("Estudante", viewResult.ViewData["UserRole"]);
        }

        [Fact]
        public async Task Index_RedirecionaLogin_SeUserNaoEncontradoNaBD()
        {
            // ARRANGE
            // Simulamos que está autenticado no cookie, mas foi apagado da BD
            _mockUserManager.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync((AppUser)null);

            var controller = GetControllerWithUser(
                new AppUser
                {
                    Id = "567",
                    UserName = "",
                    FirstName = "",
                    LastName = "",
                    Gender = Gender.Male,
                    BirthDate =  DateTime.Now.AddYears(-30),
                    Balance = 0,
                    UserCategory = new UserCategory { Name = "" }
                },
                isAuthenticated: true
            );

            // ACT
            var result = await controller.Index();

            // ASSERT
            var redirectResult = Assert.IsType<RedirectToPageResult>(result);
            Assert.Equal("/Account/Login", redirectResult.PageName);
        }
    }
}