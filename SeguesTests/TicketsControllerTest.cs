using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
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

namespace SeguesTests
{
    public class TicketsControllerTest
    {
        private readonly Mock<UserManager<AppUser>> _mockUserManager;
        private readonly Mock<RoleManager<Role>> _mockRoleManager;
        private readonly Mock<IEmailSender> _mockEmailSender;
        private readonly AppDbContext _context;
        private readonly TicketController _controller;
        private readonly AdminTicketManagementController _adminTicketController;
        private readonly TicketService _ticketService;
        private readonly AdminService _adminService;

        public TicketsControllerTest()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "TicketsTestDb_" + Guid.NewGuid())
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            _context = new AppDbContext(options);

            var usersList = new List<AppUser>();
            _mockUserManager = MockHelper.MockUserManager(usersList);
            _mockRoleManager = MockHelper.MockRoleManager<Role>();
            _mockEmailSender = new Mock<IEmailSender>();

            _ticketService = new TicketService(_context, _mockUserManager.Object, _mockRoleManager.Object);
            _adminService = new AdminService(_context, _mockUserManager.Object, _mockRoleManager.Object, _mockEmailSender.Object);

            _controller = new TicketController(_mockUserManager.Object, _mockRoleManager.Object, _ticketService, _context);
            _adminTicketController = new AdminTicketManagementController(_adminService, _mockUserManager.Object, _ticketService);

            SetupControllerContext(_controller);
            SetupControllerContext(_adminTicketController);

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

        private void SetupUserContext(Controller controller, string userId, string role, decimal balance = 100)
        {
            var category = _context.UserCategories.First(uc => uc.Name == "Estudante");

            var user = new AppUser
            {
                Id = userId,
                UserName = userId + "@test.com",
                Email = userId + "@test.com",
                Balance = balance,
                FirstName = "Test",
                LastName = "User",
                Status = UserStatus.Active,
                CreationDate = DateTime.Now,
                Gender = Gender.Male,
                BirthDate = DateTime.Now.AddYears(-20),
                UserCategory = category
            };

            if (!_context.Users.Any(u => u.Id == userId))
            {
                _context.Users.Add(user);
                _context.SaveChanges();
            }

            _mockUserManager.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(_context.Users.Include(u => u.UserCategory).First(u => u.Id == userId));

            _mockUserManager.Setup(u => u.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(userId);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, role)
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(identity);
        }

        [Fact]
        public async Task UpdatePrices_Success_UpdatesDatabase()
        {
            var userId = "admin1";
            SetupUserContext(_adminTicketController, userId, "Admin");

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
                new TicketPrice
                {
                    Id = 1,
                    Price = 5.50m,
                    UserCategory = category
                }
            };

            await _adminTicketController.UpdatePrices(updatedPrices);

            var dbPrice = await _context.TicketPrices.AsNoTracking().FirstOrDefaultAsync(p => p.Id == 1);
            Assert.Equal(5.50m, dbPrice?.Price);
        }

        [Fact]
        public async Task TransferTickets_Fails_WhenCategoriesAreDifferent()
        {
            var senderId = "sender1";
            var receiverId = "receiver1";

            var cat1 = _context.UserCategories.First(c => c.Name == "Estudante");
            var cat2 = _context.UserCategories.First(c => c.Name == "Externo");

            var sender = new AppUser
            {
                Id = senderId,
                Email = "s@s.com",
                UserName = "s@s.com",
                UserCategory = cat1,
                FirstName = "S",
                LastName = "S",
                Gender = Gender.Male,
                BirthDate = DateTime.Now.AddYears(-20),
                Status = UserStatus.Active,
                CreationDate = DateTime.Now
            };
            var receiver = new AppUser
            {
                Id = receiverId,
                Email = "r@r.com",
                UserName = "r@r.com",
                UserCategory = cat2,
                FirstName = "R",
                LastName = "R",
                Gender = Gender.Female,
                BirthDate = DateTime.Now.AddYears(-20),
                Status = UserStatus.Active,
                CreationDate = DateTime.Now
            };

            _context.Users.AddRange(sender, receiver);
            _context.SaveChanges();

            var purchase = new TicketPurchase { AppUser = sender, Quantity = 1, TransactionDate = DateTime.Now, Value = 2.5m };
            _context.TicketPurchases.Add(purchase);
            _context.SaveChanges();

            var ticket = new Ticket
            {
                ValidationCode = "T1",
                Owner = sender,
                State = TicketState.Available,
                TicketPurchase = purchase,
                ExpirationDate = DateTime.Now.AddDays(1)
            };
            _context.Tickets.Add(ticket);
            _context.SaveChanges();

            var result = await _ticketService.TransferTicketsAsync(senderId, "r@r.com", new List<string> { "T1" });

            Assert.False(result.Success);
            Assert.Contains("Transferência recusada", result.Message);
        }
    }
}