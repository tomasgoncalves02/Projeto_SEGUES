using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Moq;
using Projeto_SEGUES.Areas.Admin;
using Projeto_SEGUES.Areas.Admin.Controllers;
using Projeto_SEGUES.Areas.Admin.ViewModels;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Resources;
using Projeto_SEGUES.Services;
using SeguesTests.Helpers;
using Xunit;

namespace SeguesTests.IntegrationTests.Admin
{
    public class AdminCreateInternalAccountIntegrationTests
    {
        private AdminCreateInternalAccountController _controller;
        private Mock<ITempDataDictionary> _tempDataMock;

        private void SetupController(AppDbContext context, UserManager<AppUser> userManager, RoleManager<Role> roleManager)
        {
            var service = new AdminService(
                context,
                userManager,
                roleManager,
                new MockHelper.FakeEmailSender(),
                Mock.Of<ILogger<AdminService>>(),
                Mock.Of<IStringLocalizer<Errors>>(),
                Mock.Of<IUserService>());

            _controller = new AdminCreateInternalAccountController(service);
            _tempDataMock = new Mock<ITempDataDictionary>();
            _controller.TempData = _tempDataMock.Object;
        }

        [Fact]
        public async Task Create_Integration_FullFlow_Success()
        {
            var (context, userManager, roleManager) = MockHelper.GetIdentitySetup();

            await roleManager.CreateAsync(new Role { Id = Guid.NewGuid().ToString(), Name = "Staff", DisplayName = "Staff" });
            context.UserCategory.Add(new UserCategory { Name = "Externo" });
            await context.SaveChangesAsync();

            SetupController(context, userManager, roleManager);

            var model = new CreateInternalUserViewModel
            {
                FirstName = "Pedro",
                LastName = "Staff",
                Email = "pedro.staff@segues.pt",
                AccountType = "Staff",
                Gender = Gender.Male,
                BirthDate = DateTime.Now.AddYears(-25)
            };

            var result = await _controller.Create(model);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
        }

        [Fact]
        public async Task Create_Integration_DuplicateEmail_ReturnsViewWithErrors()
        {
            var (context, userManager, roleManager) = MockHelper.GetIdentitySetup();

            await roleManager.CreateAsync(new Role { Id = "r1", Name = "Admin", DisplayName = "Adm" });
            var cat = new UserCategory { Id = 1, Name = "Externo" };
            context.UserCategory.Add(cat);
            await context.SaveChangesAsync();

            var email = "pedro@test.pt";

            var existingUser = new AppUser
            {
                Id = "u1",
                Email = email,
                NormalizedEmail = email.ToUpper(),
                UserName = email,
                FirstName = "Pedro",
                LastName = "Jesus",
                BirthDate = DateTime.Now.AddYears(-20),
                Gender = Gender.Male,
                UserCategory = cat,
                Balance = 0,
                Status = UserStatus.Active,
                CreationDate = DateTime.Now
            };

            context.Users.Add(existingUser);
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();

            SetupController(context, userManager, roleManager);

            var model = new CreateInternalUserViewModel
            {
                FirstName = "Pedro",
                LastName = "Novo",
                Email = email,
                AccountType = "Admin",
                Gender = Gender.Male,
                BirthDate = DateTime.Now.AddYears(-20)
            };

            var result = await _controller.Create(model);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Index", viewResult.ViewName);
            _tempDataMock.VerifySet(t => t[It.IsAny<string>()] = It.IsAny<object>(), Times.AtLeastOnce);
        }
    }
}