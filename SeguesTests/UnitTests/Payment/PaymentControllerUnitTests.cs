using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Moq;
using Projeto_SEGUES.Areas.Payment;
using Projeto_SEGUES.Areas.Payment.ViewModels;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Projeto_SEGUES.Areas.Payment.Controllers;
using Xunit;

namespace SeguesTests.UnitTests.Payment
{
    public class PaymentControllerUnitTests
    {
        private readonly Mock<UserManager<AppUser>> _mockUserManager;
        private readonly Mock<ILogger<PaymentController>> _mockLogger;
        private readonly Mock<IPaymentService> _mockPaymentService;
        private readonly PaymentController _controller;

        public PaymentControllerUnitTests()
        {
            var store = new Mock<IUserStore<AppUser>>();
            _mockUserManager = new Mock<UserManager<AppUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
            _mockLogger = new Mock<ILogger<PaymentController>>();
            _mockPaymentService = new Mock<IPaymentService>();

            _controller = new PaymentController(
                _mockUserManager.Object,
                _mockLogger.Object,
                _mockPaymentService.Object);

            var httpContext = new DefaultHttpContext();
            _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            _controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());

            var mockUrlHelper = new Mock<IUrlHelper>();
            mockUrlHelper
                .Setup(x => x.Action(It.IsAny<UrlActionContext>()))
                .Returns("http://localhost/callback-url");
            _controller.Url = mockUrlHelper.Object;
        }

        [Fact]
        public void Deposit_ReturnsView()
        {
            var result = _controller.Deposit();
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public async Task CreateCheckoutSession_ValidModel_RedirectsToStripe()
        {
            var user = new AppUser
            {
                Id = "pedro-77",
                UserName = "Pedro",
                FirstName = "Pedro",
                LastName = "Jesus",
                BirthDate = new DateTime(2000, 1, 1),
                Gender = Projeto_SEGUES.Models.Enums.Gender.Male,
                UserCategory = new Projeto_SEGUES.Models.User.UserCategory { Name = "Estudante" }
            };

            var model = new DepositAmountViewModel { Amount = 10.50m };

            _mockUserManager.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
            _mockPaymentService.Setup(s => s.CreateStripeSessionAsync(user, 10.50m, It.IsAny<string>(), It.IsAny<string>()))
                               .ReturnsAsync("https://stripe.com/checkout/fake-session");

            var result = await _controller.CreateCheckoutSession(model);

            var redirectResult = Assert.IsType<RedirectResult>(result);
            Assert.Equal("https://stripe.com/checkout/fake-session", redirectResult.Url);
        }

        [Fact]
        public async Task SuccessPayment_ValidParams_ReturnsRedirectWithSuccessMessage()
        {
            var resultData = new ServiceResult(true, "Sucesso, Pedro!");

            _mockPaymentService.Setup(s => s.ProcessPaymentSuccessAsync("REF123", "SESS456"))
                               .ReturnsAsync(resultData);

            var result = await _controller.SuccessPayment("REF123", "SESS456");

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Home", redirectResult.ControllerName);

            Assert.Contains(_controller.TempData.Values, v => v?.ToString()?.Contains("Pedro") ?? false);
        }

        [Fact]
        public void CancelPayment_RedirectsToHomeWithInfo()
        {
            var result = _controller.CancelPayment();

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Home", redirectResult.ControllerName);
        }
    }
}