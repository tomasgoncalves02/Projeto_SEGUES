using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Data.Sqlite;
using Moq;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models.Enums;
using System.Security.Claims;
using Projeto_SEGUES.Areas.Ticket;
using Projeto_SEGUES.Areas.Admin;
using Projeto_SEGUES.Models.Ticket;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;
using SeguesTests.Helpers;
using Xunit;
using Projeto_SEGUES.Areas.Admin.ViewModels;

namespace SeguesTests
{
    public class TicketsControllerTest : IDisposable
    {
        private readonly Mock<UserManager<AppUser>> _mockUserManager;
        private readonly Mock<RoleManager<Role>> _mockRoleManager;
        private readonly Mock<IEmailSender> _mockEmailSender;
        private readonly AppDbContext _context;
        private readonly SqliteConnection _connection;
        private readonly TicketController _ticketController;
        private readonly AdminTicketManagementController _adminTicketController;
        private readonly AdminUserManagementController _userManagementController;
        private readonly TicketService _ticketService;
        private readonly AdminService _adminService;

        public TicketsControllerTest()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(_connection)
                .Options;

            _context = new AppDbContext(options);
            _context.Database.EnsureCreated();

            var usersList = new List<AppUser>();
            _mockUserManager = MockHelper.MockUserManager(usersList);
            _mockRoleManager = MockHelper.MockRoleManager<Role>();
            _mockEmailSender = new Mock<IEmailSender>();

            _ticketService = new TicketService(_context, _mockUserManager.Object, _mockRoleManager.Object);
            _adminService = new AdminService(_context, _mockUserManager.Object, _mockRoleManager.Object, _mockEmailSender.Object);

            _ticketController = new TicketController(_mockUserManager.Object, _mockRoleManager.Object, _ticketService, _context);
            _adminTicketController = new AdminTicketManagementController(_adminService, _mockUserManager.Object, _ticketService);
            _userManagementController = new AdminUserManagementController(_mockUserManager.Object, _adminService);

            SetupControllerContext(_ticketController);
            SetupControllerContext(_adminTicketController);
            SetupControllerContext(_userManagementController);

            if (!_context.UserCategories.Any())
            {
                _context.UserCategories.AddRange(
                    new UserCategory { Id = 1, Name = "Estudante" },
                    new UserCategory { Id = 2, Name = "Externo" },
                    new UserCategory { Id = 3, Name = "Trabalhador IPS" }
                );
                _context.SaveChanges();
            }
        }

        private void SetupControllerContext(Controller controller)
        {
            var httpContext = new DefaultHttpContext();
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
        }

        private AppUser SetupUser(string id, string email, string categoryName, decimal balance = 100)
        {
            var category = _context.UserCategories.First(c => c.Name == categoryName);
            var user = new AppUser
            {
                Id = id,
                Email = email,
                UserName = email,
                FirstName = "User",
                LastName = "Test",
                Balance = balance,
                Gender = Gender.Male,
                BirthDate = DateTime.Now.AddYears(-20),
                Status = UserStatus.Active,
                CreationDate = DateTime.Now,
                UserCategory = category
            };
            _context.Users.Add(user);
            _context.SaveChanges();
            return user;
        }

        [Fact]
        public async Task RF22_Consulta_Historico_Filtro_Data()
        {
            var user = SetupUser("u22", "u22@test.com", "Estudante");

            var purchase = new TicketPurchase
            {
                AppUser = user,
                Quantity = 1,
                TransactionDate = DateTime.Now.AddDays(-5),
                Value = 10m
            };
            _context.TicketPurchases.Add(purchase);
            _context.SaveChanges();

            _context.Tickets.Add(new Ticket
            {
                ValidationCode = "HIST_OK",
                Owner = user,
                State = TicketState.Available,
                TicketPurchase = purchase,
                ExpirationDate = DateTime.Now.AddDays(5)
            });
            _context.SaveChanges();

            var filterDate = DateTime.Today.AddDays(-10);
            var history = await _ticketService.QueryHistoryAsync(user.Id, "", null, "", filterDate);

            Assert.NotEmpty(history);
            Assert.Contains(history, t => t.ValidationCode == "HIST_OK");
        }

        [Fact]
        public async Task Index_ReturnsMyTickets()
        {
            var user = SetupUser("u_idx", "idx@t.com", "Estudante");
            var purchase = new TicketPurchase { AppUser = user, Quantity = 1, TransactionDate = DateTime.Now, Value = 0m };
            _context.TicketPurchases.Add(purchase);

            var ticket = new Ticket
            {
                Owner = user,
                TicketPurchase = purchase,
                State = TicketState.Available,
                ExpirationDate = DateTime.Now.AddDays(2),
                ValidationCode = "ACT1"
            };

            _context.Tickets.Add(ticket);
            _context.SaveChanges();

            _mockUserManager.Setup(u => u.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(user.Id);
            _mockUserManager.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);

            var result = await _ticketController.Index();
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public async Task UpdatePrices_Success_UpdatesDatabase()
        {
            var category = _context.UserCategories.First(uc => uc.Name == "Estudante");
            var price = new TicketPrice
            {
                Id = 1,
                UserCategory = category,
                Price = 1.00m,
                InitialDatePrice = DateTime.Now.AddDays(-1),
                EndDatePrice = DateTime.Now.AddDays(1)
            };
            _context.TicketPrices.Add(price);
            _context.SaveChanges();

            var updatedPrices = new List<TicketPrice>
            {
                new TicketPrice { Id = 1, Price = 5.50m, UserCategory = category }
            };

            await _adminTicketController.UpdatePrices(updatedPrices);

            var dbPrice = await _context.TicketPrices.AsNoTracking().FirstOrDefaultAsync(p => p.Id == 1);
            Assert.Equal(5.50m, dbPrice?.Price);
        }

        [Fact]
        public async Task Transferencia_Sucesso_Mesma_Categoria()
        {
            var sender = SetupUser("s1", "s1@test.com", "Estudante");
            var receiver = SetupUser("r1", "r1@test.com", "Estudante");

            var purchase = new TicketPurchase { AppUser = sender, Quantity = 1, TransactionDate = DateTime.Now, Value = 0m };
            _context.TicketPurchases.Add(purchase);

            var ticket = new Ticket { ValidationCode = "TK04", Owner = sender, State = TicketState.Available, TicketPurchase = purchase, ExpirationDate = DateTime.Now.AddDays(1) };
            _context.Tickets.Add(ticket);
            _context.SaveChanges();

            var result = await _ticketService.TransferTicketsAsync(sender.Id, receiver.Email, new List<string> { "TK04" });

            Assert.True(result.Success);
            var dbTicket = await _context.Tickets.AsNoTracking().Include(t => t.Owner).FirstAsync(t => t.ValidationCode == "TK04");
            Assert.Equal(receiver.Id, dbTicket.Owner.Id);
        }

        [Fact]
        public async Task Transferencia_Bloqueada_Categorias_Diferentes()
        {
            var sender = SetupUser("s29", "s29@test.com", "Estudante");
            var receiver = SetupUser("r29", "r29@test.com", "Externo");

            var purchase = new TicketPurchase { AppUser = sender, Quantity = 1, TransactionDate = DateTime.Now, Value = 0m };
            _context.TicketPurchases.Add(purchase);

            var ticket = new Ticket { ValidationCode = "TK29", Owner = sender, State = TicketState.Available, TicketPurchase = purchase, ExpirationDate = DateTime.Now.AddDays(1) };
            _context.Tickets.Add(ticket);
            _context.SaveChanges();

            var result = await _ticketService.TransferTicketsAsync(sender.Id, receiver.Email, new List<string> { "TK29" });

            Assert.False(result.Success);
            Assert.Contains("Transferência recusada", result.Message);
        }

        [Fact]
        public async Task Validacao_Senha_Sucesso()
        {
            var admin = SetupUser("adm1", "adm@test.com", "Trabalhador IPS");
            var owner = SetupUser("own1", "own@test.com", "Estudante");

            var purchase = new TicketPurchase { AppUser = owner, Quantity = 1, TransactionDate = DateTime.Now, Value = 0m };
            _context.TicketPurchases.Add(purchase);

            var ticket = new Ticket { ValidationCode = "QR15", Owner = owner, State = TicketState.Available, TicketPurchase = purchase, ExpirationDate = DateTime.Now.AddDays(1) };
            _context.Tickets.Add(ticket);
            _context.SaveChanges();

            var result = await _ticketService.ValidateTicketAsync("QR15", admin);

            Assert.True(result.Success);
            var dbTicket = await _context.Tickets.AsNoTracking().FirstAsync(t => t.ValidationCode == "QR15");
            Assert.Equal(TicketState.Used, dbTicket.State);
        }

        [Fact]
        public async Task Edicao_Dados_Pessoais_Sucesso()
        {
            var user = SetupUser("u20", "u20@test.com", "Estudante");

            _mockUserManager.Setup(m => m.FindByIdAsync(user.Id)).ReturnsAsync(user);
            _mockUserManager.Setup(m => m.UpdateAsync(It.IsAny<AppUser>())).ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Client" });
            _mockUserManager.Setup(m => m.RemoveFromRolesAsync(user, It.IsAny<IEnumerable<string>>())).ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(m => m.AddToRoleAsync(user, It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);

            var model = new EditUserViewModel
            {
                Id = user.Id,
                FirstName = "Editado",
                LastName = "Silva",
                Email = "u20@test.com",
                Gender = Gender.Male,
                BirthDate = DateTime.Now.AddYears(-25),
                Category = "Estudante",
                Role = "Client",
                Balance = 50
            };

            var result = await _userManagementController.Edit(model);

            Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Editado", user.FirstName);
        }

        [Fact]
        public async Task Carregamento_Saldo_Sucesso()
        {
            var user = SetupUser("u35", "u35@test.com", "Estudante", balance: 10.00m);
            decimal valorCarregar = 20.00m;

            user.Balance += valorCarregar;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            var dbUser = await _context.Users.AsNoTracking().FirstAsync(u => u.Id == "u35");
            Assert.Equal(30.00m, dbUser.Balance);
        }

        public void Dispose()
        {
            _connection.Close();
            _context.Dispose();
        }
    }
}