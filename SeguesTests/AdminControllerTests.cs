using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using Projeto_SEGUES.Controllers;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Tests.Helpers;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Projeto_SEGUES.Tests
{
    public class AdminControllerTests
    {
        private readonly Mock<UserManager<User>> _mockUserManager;
        private readonly Mock<RoleManager<IdentityRole>> _mockRoleManager;
        private readonly Mock<IEmailSender> _mockEmailSender;
        private readonly Mock<AppDbContext> _mockContext;
        private readonly AdminController _controller;

        public AdminControllerTests()
        {
            // 1. Configurar Mocks
            var usersList = new List<User>();
            _mockUserManager = MockHelper.MockUserManager(usersList);
            _mockRoleManager = MockHelper.MockRoleManager<IdentityRole>();
            _mockEmailSender = new Mock<IEmailSender>();

            // Configurar DbContext InMemory
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDb_" + System.Guid.NewGuid()) // Nome único para evitar conflitos
                .Options;
            _mockContext = new Mock<AppDbContext>(options);

            // 2. Inicializar Controller
            _controller = new AdminController(
                _mockUserManager.Object,
                _mockRoleManager.Object,
                _mockContext.Object,
                _mockEmailSender.Object
            );

            // 3. Configurar TempData
            _controller.TempData = new TempDataDictionary(
                new DefaultHttpContext(),
                Mock.Of<ITempDataProvider>()
            );

            // --- CORREÇÃO DO ERRO NULL REFERENCE ---
            // Simulamos um HttpContext para que o "Request.Scheme" e "Request.Host" funcionem
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Scheme = "https";
            httpContext.Request.Host = new HostString("localhost");

            _controller.ControllerContext = new ControllerContext()
            {
                HttpContext = httpContext
            };
        }

        // ... (O resto dos testes mantém-se igual) ...

        [Fact]
        public async Task CreateInternalAccount_DeveCriarUser_E_EnviarEmail_SemRedirecionar()
        {
            // ARRANGE
            var model = new CreateInternalUserViewModel
            {
                FirstName = "Maria",
                LastName = "Silva",
                Email = "maria.silva@escola.pt",
                AccountType = "Admin",
                Gender = Enums.Gender.Female
            };

            // Simular sucesso na criação e na role
            _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);

            _mockUserManager.Setup(x => x.AddToRoleAsync(It.IsAny<User>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);

            // ACT
            var result = await _controller.CreateInternalAccount(model);

            // ASSERT
            _mockUserManager.Verify(u => u.CreateAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Once);
            _mockEmailSender.Verify(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.True(_controller.TempData.ContainsKey("Success"));
        }

        [Fact]
        public async Task Edit_DeveAtualizarUser_E_Redirecionar()
        {
            // ARRANGE
            var userId = "user-123";
            var existingUser = new User
            {
                Id = userId,
                Email = "antigo@mail.com",
                FirstName = "Antigo",
                Role = Enums.UserRole.Employee
            };

            _mockUserManager.Setup(u => u.FindByIdAsync(userId)).ReturnsAsync(existingUser);
            _mockUserManager.Setup(u => u.UpdateAsync(It.IsAny<User>())).ReturnsAsync(IdentityResult.Success);
            _mockRoleManager.Setup(r => r.RoleExistsAsync(It.IsAny<string>())).ReturnsAsync(true);
            _mockUserManager.Setup(u => u.AddToRoleAsync(It.IsAny<User>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(u => u.RemoveFromRolesAsync(It.IsAny<User>(), It.IsAny<IEnumerable<string>>())).ReturnsAsync(IdentityResult.Success);

            var model = new EditUserViewModel
            {
                Id = userId,
                Email = "novo@mail.com",
                FirstName = "NovoNome",
                LastName = "Sobrenome",
                Balance = 10,
                Role = "Admin",
                Gender = Enums.Gender.Male
            };

            // ACT
            var result = await _controller.Edit(model);

            // ASSERT
            _mockUserManager.Verify(u => u.UpdateAsync(It.Is<User>(u => u.FirstName == "NovoNome")), Times.Once);
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("ListUsers", redirectResult.ActionName);
        }

        [Fact]
        public async Task DeleteConfirmed_DeveApagarUser_E_RedirecionarIndex()
        {
            // ARRANGE
            var userId = "user-delete";
            var user = new User { Id = userId };

            _mockUserManager.Setup(u => u.FindByIdAsync(userId)).ReturnsAsync(user);
            _mockUserManager.Setup(u => u.DeleteAsync(user)).ReturnsAsync(IdentityResult.Success);

            // ACT
            var result = await _controller.DeleteConfirmed(userId);

            // ASSERT
            _mockUserManager.Verify(u => u.DeleteAsync(user), Times.Once);
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
        }
    }
}