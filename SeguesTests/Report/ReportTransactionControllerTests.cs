using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Projeto_SEGUES.Areas.Report.Controllers;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.Order;
using Projeto_SEGUES.Models.Payment;
using Projeto_SEGUES.Models.User;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Xunit;

namespace SeguesTests.Report
{
    public class ReportTransactionControllerTests
    {
        private readonly Mock<UserManager<AppUser>> _mockUserManager;
        private readonly AppDbContext _context;
        private readonly ReportTransactionController _controller;

        public ReportTransactionControllerTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _context = new AppDbContext(options);

            var store = new Mock<IUserStore<AppUser>>();
            _mockUserManager = new Mock<UserManager<AppUser>>(store.Object, null, null, null, null, null, null, null, null);

            _controller = new ReportTransactionController(_mockUserManager.Object, _context);

            var httpContext = new DefaultHttpContext();
            _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        }

        private AppUser CreateValidTestUser(string id) => new()
        {
            Id = id,
            FirstName = "Diogo",
            LastName = "User",
            Email = "diogo@test.com",
            BirthDate = DateTime.Now.AddYears(-20),
            Gender = Gender.Male,
            UserCategory = new UserCategory { Name = "Estudante" }
        };

        // Verifies the index view returns the full transaction history for the authenticated user
        [Fact]
        public async Task Index_AuthenticatedUser_ReturnsViewWithTransactions()
        {
            var user = CreateValidTestUser("user-1");
            _context.Users.Add(user);
            _context.Transaction.Add(new Transaction { User = user, Amount = 10, Reference = "REF1", CreatedAt = DateTime.Now });
            await _context.SaveChangesAsync();

            _mockUserManager.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);

            var result = await _controller.Index();

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<Transaction>>(viewResult.Model);
            Assert.Single(model);
        }

        // Returns a ChallengeResult when the user session is invalid during index access
        [Fact]
        public async Task Index_UserNotFound_ReturnsChallenge()
        {
            _mockUserManager.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync((AppUser)null!);

            var result = await _controller.Index();

            Assert.IsType<ChallengeResult>(result);
        }

        // Ensures the filtered balance returns the correct partial view for HTMX updates
        [Fact]
        public async Task GetFilteredBalance_ReturnsPartialView()
        {
            var user = CreateValidTestUser("user-1");
            _mockUserManager.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);

            var result = await _controller.GetFilteredBalance("", "", null);

            var partialViewResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("_BalanceHistoryRows", partialViewResult.ViewName);
        }

        // Verifies that the search filter correctly matches transaction descriptions or references
        [Fact]
        public async Task GetFilteredBalance_SearchFilter_ReturnsMatchingResults()
        {
            var user = CreateValidTestUser("user-1");
            _context.Users.Add(user);
            _context.Transaction.Add(new Transaction { User = user, Amount = 5, Reference = "TargetRef", Description = "Coffee", CreatedAt = DateTime.Now });
            _context.Transaction.Add(new Transaction { User = user, Amount = 5, Reference = "Other", Description = "Tea", CreatedAt = DateTime.Now });
            await _context.SaveChangesAsync();

            _mockUserManager.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);

            var result = await _controller.GetFilteredBalance("Target", "", null);

            var model = Assert.IsAssignableFrom<IEnumerable<Transaction>>(((PartialViewResult)result).Model);
            Assert.Single(model);
        }

        // Confirms the type filter correctly separates deposits (Entrada) from expenses (Saida)
        [Fact]
        public async Task GetFilteredBalance_TypeFilter_FiltersByAmountDirection()
        {
            var user = CreateValidTestUser("user-1");
            _context.Users.Add(user);
            _context.Transaction.Add(new Transaction { User = user, Amount = 15.0m, Reference = "R1", CreatedAt = DateTime.Now }); // Entrada
            _context.Transaction.Add(new Transaction { User = user, Amount = -5.0m, Reference = "R2", CreatedAt = DateTime.Now }); // Saida
            await _context.SaveChangesAsync();

            _mockUserManager.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);

            var result = await _controller.GetFilteredBalance("", "Saida", null);

            var model = Assert.IsAssignableFrom<IEnumerable<Transaction>>(((PartialViewResult)result).Model);
            Assert.Single(model);
            Assert.True(model.First().Amount < 0);
        }

        // Ensures the date filter correctly excludes transactions before the specified starting date
        [Fact]
        public async Task GetFilteredBalance_DateFilter_ReturnsTransactionsFromDate()
        {
            var user = CreateValidTestUser("user-1");
            _context.Users.Add(user);
            _context.Transaction.Add(new Transaction { User = user, Amount = 10, Reference = "Old", CreatedAt = DateTime.Now.AddDays(-10) });
            _context.Transaction.Add(new Transaction { User = user, Amount = 10, Reference = "New", CreatedAt = DateTime.Now });
            await _context.SaveChangesAsync();

            _mockUserManager.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);

            var result = await _controller.GetFilteredBalance("", "", DateTime.Now.AddDays(-1));

            var model = Assert.IsAssignableFrom<IEnumerable<Transaction>>(((PartialViewResult)result).Model);
            Assert.Single(model);
            Assert.Equal("New", model.First().Reference);
        }


        // Verifies that the order model fails validation when the total value is negative, ensuring strict financial data integrity
        [Fact]
        public void Order_Validation_FailsWithNegativeTotalValue()
        {
            var order = new Order
            {
                TotalValue = -10.00m,
                AppUser = new AppUser
                {
                    FirstName = "Diogo",
                    LastName = "User",
                    BirthDate = DateTime.Now.AddYears(-20),
                    Gender = Gender.Male,
                    Email = "diogo@test.com",
                    UserCategory = new UserCategory
                    {
                        Id = 1,
                        Name = "Estudante"
                       
                    }
                }
            };

            var context = new ValidationContext(order);
            var results = new List<ValidationResult>();

            var isValid = Validator.TryValidateObject(order, context, results, true);

            Assert.False(isValid);
            Assert.Contains(results, v => v.MemberNames.Contains("TotalValue"));
        }
    }
}