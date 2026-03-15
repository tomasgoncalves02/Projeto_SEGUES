using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using Projeto_SEGUES.Areas.Payment;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models.Payment;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Models.Enums;
using System.Security.Claims;
using Xunit;

namespace SeguesTests.Payment
{
    public class PaymentControllerTests
    {
        private readonly Mock<UserManager<AppUser>> _mockUserManager;
        private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
        private readonly AppDbContext _context;
        private readonly PaymentController _controller;

        public PaymentControllerTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _context = new AppDbContext(options);

            var store = new Mock<IUserStore<AppUser>>();
            _mockUserManager = new Mock<UserManager<AppUser>>(store.Object, null, null, null, null, null, null, null, null);
            _mockHttpClientFactory = new Mock<IHttpClientFactory>();

            _controller = new PaymentController(_context, _mockHttpClientFactory.Object, _mockUserManager.Object);

            var httpContext = new DefaultHttpContext();
            _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            _controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
        }

        private AppUser CreateValidTestUser() => new()
        {
            Id = "user-pay",
            FirstName = "Diogo",
            LastName = "Payment",
            Email = "pay@test.com",
            BirthDate = DateTime.Now.AddYears(-20),
            Gender = Gender.Male,
            UserCategory = new UserCategory { Name = "Estudante" },
            Balance = 0.00m
        };

        // Confirms that the deposit view is correctly returned to the user
        [Fact]
        public void Deposit_ReturnsView()
        {
            var result = _controller.Deposit();
            Assert.IsType<ViewResult>(result);
        }

        // Returns a BadRequest when trying to create a session with a non-positive amount
        [Fact]
        public async Task CreateCheckoutSession_InvalidAmount_ReturnsBadRequest()
        {
            var result = await _controller.CreateCheckoutSession(0);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        // Redirects to the challenge result if an unauthenticated user attempts to deposit
        [Fact]
        public async Task CreateCheckoutSession_UnauthenticatedUser_ReturnsChallenge()
        {
            _mockUserManager.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync((AppUser)null!);

            var result = await _controller.CreateCheckoutSession(10);

            Assert.IsType<ChallengeResult>(result);
        }

        // Successfully updates user balance and marks transaction as paid upon return from Stripe
        [Fact]
        public async Task SuccessPayment_ValidReference_UpdatesBalanceAndRedirects()
        {
            var user = CreateValidTestUser();
            _context.Users.Add(user);

            var transaction = new Transaction
            {
                User = user,
                Amount = 20.00m,
                Reference = "REF123",
                IsPaid = false
            };
            _context.Set<Transaction>().Add(transaction);
            await _context.SaveChangesAsync();

            var result = await _controller.SuccessPayment("REF123");

            var updatedUser = await _context.Users.FindAsync(user.Id);
            var updatedTransaction = await _context.Transaction.FirstAsync(t => t.Reference == "REF123");

            Assert.Equal(20.00m, updatedUser!.Balance);
            Assert.True(updatedTransaction.IsPaid);
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
        }

        // Returns an error message if the payment reference is invalid or already processed
        [Fact]
        public async Task SuccessPayment_InvalidReference_ShowsError()
        {
            var result = await _controller.SuccessPayment("INVALID");

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Contains("error", _controller.TempData.Values.FirstOrDefault()?.ToString()?.ToLower());
        }

        // Handles payment cancellation by redirecting home with a specific message
        [Fact]
        public void CancelPayment_RedirectsHomeWithMessage()
        {
            var result = _controller.CancelPayment();

            Assert.IsType<RedirectToActionResult>(result);
            Assert.Contains("cancelado", _controller.TempData.Values.FirstOrDefault()?.ToString()?.ToLower());
        }
    }
}