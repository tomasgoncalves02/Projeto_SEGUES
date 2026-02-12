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

namespace SeguesTests
{
    public class AdminControllerTests
    {
        private readonly Mock<UserManager<AppUser>> _mockUserManager;
        private readonly Mock<RoleManager<Role>> _mockRoleManager;
        private readonly Mock<IEmailSender> _mockEmailSender;
        private readonly AppDbContext _context;
        private readonly AdminController _controller;
        private readonly AdminCreateInternalAccountController _createInternalAccountController;
        private readonly AdminUserManagementController _userManagementController;
        private readonly AdminTicketManagementController _ticketManagementController;
        private readonly EmployeeController _employeeController;
        private readonly Mock<IAdminService> _mockAdminService;
        private readonly Mock<ITicketService> _mockTicketService;

        public AdminControllerTests()
        {
            // 1. Configurar Mocks
            var usersList = new List<AppUser>();
            _mockUserManager = MockHelper.MockUserManager(usersList);
            _mockRoleManager = MockHelper.MockRoleManager<Role>();
            _mockEmailSender = new Mock<IEmailSender>();
            _mockAdminService = new Mock<IAdminService>();
            _mockTicketService = new Mock<ITicketService>();

            // Configurar DbContext InMemory
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDb_" + System.Guid.NewGuid()) // Nome único para evitar conflitos
                .Options;
            _context = new AppDbContext(options);
            
            _context.UserCategories.Add(new UserCategory { Name = "Externo" });
            _context.SaveChanges();

            // 2. Inicializar Controller
            _controller = new AdminController();
            _createInternalAccountController = new AdminCreateInternalAccountController(_mockAdminService.Object);
            _userManagementController = new AdminUserManagementController(_mockUserManager.Object, _mockAdminService.Object);
            _ticketManagementController = new AdminTicketManagementController(_mockAdminService.Object, _mockTicketService.Object);
            _employeeController = new EmployeeController();

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
                Gender = Gender.Female,
                BirthDate = DateTime.Now.AddYears(-28)
            };

            // Simular sucesso na criação e na role
            _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<AppUser>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);
            _mockRoleManager.Setup(x => x.RoleExistsAsync(It.IsAny<string>()))
                .ReturnsAsync(true);
            _mockUserManager.Setup(x => x.AddToRoleAsync(It.IsAny<AppUser>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(x => x.GetRolesAsync(It.IsAny<AppUser>()))
                .ReturnsAsync(new List<string> { "Admin" });
            _mockEmailSender.Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // ACT
            var result = await _createInternalAccountController.Create(model);
            await _context.SaveChangesAsync();

            // ASSERT
            _mockUserManager.Verify(u => u.CreateAsync(It.IsAny<AppUser>(), It.IsAny<string>()), Times.Once);
            _mockEmailSender.Verify(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.True(_controller.TempData.ContainsKey("Success"));
        }

        [Fact]
        public async Task Edit_DeveAtualizarUser_E_Redirecionar()
        {
            // ARRANGE
            var userId = "user-123";
            var category = await _context.UserCategories.FirstOrDefaultAsync(uc => uc.Name == "Externo");
            var existingUser = new AppUser
            {
                Id = userId,
                Email = "antigo@mail.com",
                FirstName = "Antigo",
                LastName = "Nome",
                UserCategory = category!,
                Gender = Gender.Male,
                BirthDate = DateTime.Now.AddYears(-30)
            };
            
            // Employee role
            _mockUserManager.Setup(u => u.FindByIdAsync(userId)).ReturnsAsync(existingUser);
            _mockUserManager.Setup(u => u.UpdateAsync(It.IsAny<AppUser>())).ReturnsAsync(IdentityResult.Success);
            _mockRoleManager.Setup(r => r.RoleExistsAsync(It.IsAny<string>())).ReturnsAsync(true);
            _mockUserManager.Setup(u => u.AddToRoleAsync(It.IsAny<AppUser>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(u => u.RemoveFromRolesAsync(It.IsAny<AppUser>(), It.IsAny<IEnumerable<string>>())).ReturnsAsync(IdentityResult.Success);

            var model = new EditUserViewModel
            {
                Id = userId,
                Email = "novo@mail.com",
                FirstName = "NovoNome",
                LastName = "Sobrenome",
                Balance = 10,
                Role = "Admin",
                Gender = Gender.Male,
                BirthDate = DateTime.Now.AddYears(-30),
            };

            // ACT
            var result = await _userManagementController.Edit(model);

            // ASSERT
            _mockUserManager.Verify(u => u.UpdateAsync(It.Is<AppUser>(u => u.FirstName == "NovoNome")), Times.Once);
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("ListUsers", redirectResult.ActionName);
        }

        [Fact]
        public async Task DeleteConfirmed_DeveApagarUser_E_RedirecionarListUsers()
        {
            // ARRANGE
            var userId = "937";
            var user = new AppUser
            {
                Id = userId,
                Email = "",
                FirstName = "",
                LastName = "",
                Gender = Gender.Male,
                BirthDate = DateTime.Now.AddYears(-25),
                UserCategory = new UserCategory { Name = "" }
            };

            _mockUserManager.Setup(u => u.FindByIdAsync(userId)).ReturnsAsync(user);
            _mockUserManager.Setup(u => u.DeleteAsync(user)).ReturnsAsync(IdentityResult.Success);

            // ACT
            var result = await _userManagementController.DeleteConfirmed(userId);

            // ASSERT
            _mockUserManager.Verify(u => u.DeleteAsync(user), Times.Once);
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("ListUsers", redirectResult.ActionName);
        }
    }
}