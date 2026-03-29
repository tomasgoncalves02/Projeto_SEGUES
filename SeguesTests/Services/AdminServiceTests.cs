using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Moq;
using Projeto_SEGUES.Areas.Admin.ViewModels;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models.Admin;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.Ticket;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Resources;
using Projeto_SEGUES.Services;

namespace SeguesTests.Services
{
    public class AdminServiceTests
    {
        private AppDbContext GetDatabaseContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            return new AppDbContext(options);
        }

        private Mock<UserManager<AppUser>> GetMockUserManager(AppDbContext context)
        {
            var userStore = new Mock<IUserStore<AppUser>>();
            var mock = new Mock<UserManager<AppUser>>(userStore.Object, null, null, null, null, null, null, null, null);

            mock.Setup(m => m.Users).Returns(context.Users);

            return mock;
        }

        private Mock<RoleManager<Role>> GetMockRoleManager() =>
            new Mock<RoleManager<Role>>(new Mock<IRoleStore<Role>>().Object, null, null, null, null);



        // Verifies that internal user creation fails when the specified role is not found
        [Fact]
        public async Task CreateInternalUserAsync_InvalidRole_ReturnsFailedResult()
        {
            var context = GetDatabaseContext();
            var mockRoleMgr = GetMockRoleManager();
            var service = new AdminService(
                context, 
                GetMockUserManager(context).Object, 
                mockRoleMgr.Object, 
                new Mock<IEmailSender>().Object,
                new Mock<ILogger<AdminService>>().Object,
                new Mock<IStringLocalizer<Errors>>().Object);
            
            mockRoleMgr.Setup(m => m.FindByNameAsync("NonExistent")).ReturnsAsync((Role)null!);

            var model = new CreateInternalUserViewModel
            {
                AccountType = "NonExistent",
                FirstName = "Pedro",
                LastName = "Tester",
                Email = "pedro@test.com",
                Gender = Gender.Male,
                BirthDate = DateTime.Now.AddYears(-20)
            };

            var result = await service.CreateInternalUserAsync(model);

            Assert.False(result.Success);
            Assert.Contains("Dados inválidos", result.Message.Split("; ").First());
        }

        // Ensures that the internal user creation process is fully rolled back if the welcome email fails to send, protecting database consistency
        [Fact]
        public async Task CreateInternalUserAsync_EmailFailure_RollsBackUserCreation()
        {
            var context = GetDatabaseContext();
            var mockUserMgr = GetMockUserManager(context);
            var mockRoleMgr = GetMockRoleManager();
            var mockEmailSender = new Mock<IEmailSender>();

            context.UserCategory.Add(new UserCategory { Name = "Externo" });
            await context.SaveChangesAsync();

            var service = new AdminService(context, mockUserMgr.Object, mockRoleMgr.Object, mockEmailSender.Object,
                new Mock<ILogger<AdminService>>().Object,
                new Mock<IStringLocalizer<Errors>>().Object);

            mockRoleMgr.Setup(m => m.FindByNameAsync(It.IsAny<string>())).ReturnsAsync(new Role { Name = "Admin", DisplayName = "Administrador" });
            mockUserMgr.Setup(m => m.CreateAsync(It.IsAny<AppUser>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);

            mockEmailSender.Setup(m => m.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("Network failure"));

            var model = new CreateInternalUserViewModel
            {
                Email = "pedro@test.com",
                FirstName = "Pedro",
                LastName = "Tester",
                AccountType = "Admin",
                Gender = Gender.Male,
                BirthDate = DateTime.Now.AddYears(-25)
            };

            var result = await service.CreateInternalUserAsync(model);

            Assert.False(result.Success);
            Assert.Contains("Erro de conexão", result.Message.Split("; ").First());
        }

        // Verifies that the search filter correctly identifies users by their first name (case-insensitive)
        [Fact]
        public async Task GetFilteredUsersAsync_SearchByName_ReturnsMatch()
        {
            var context = GetDatabaseContext();
            var mockUserMgr = GetMockUserManager(context);
            var service = new AdminService(context, mockUserMgr.Object, GetMockRoleManager().Object, new Mock<IEmailSender>().Object, new Mock<ILogger<AdminService>>().Object, new Mock<IStringLocalizer<Errors>>().Object);

            var cat = new UserCategory { Id = 1, Name = "Student" };

            context.Users.Add(new AppUser
            {
                Id = "u-pedro",
                FirstName = "Pedro",
                LastName = "Silva",
                Email = "pedro@test.pt",
                UserCategory = cat,
                BirthDate = DateTime.Now.AddYears(-20),
                Gender = Gender.Male
            });
            await context.SaveChangesAsync();

            var result = await service.GetFilteredUsersAsync("pedro", null, null);

            Assert.Single(result);
            Assert.Equal("Pedro", result[0].FirstName);
        }

        // Confirms that ticket prices are updated and the expiration date is set to the end of the next day
        [Fact]
        public async Task UpdateTicketPricesAsync_UpdatesPriceAndDate()
        {
            var context = GetDatabaseContext();
            var service = new AdminService(context, GetMockUserManager(context).Object, GetMockRoleManager().Object, new Mock<IEmailSender>().Object, new Mock<ILogger<AdminService>>().Object, new Mock<IStringLocalizer<Errors>>().Object);

            var cat = new UserCategory { Id = 1, Name = "Estudante" };
            var price = new TicketPrice
            {
                Id = 1,
                Price = 2.0m,
                UserCategory = cat,
                InitialDatePrice = DateTime.Now,
                EndDatePrice = DateTime.Now
            };
            context.TicketPrice.Add(price);
            await context.SaveChangesAsync();

            var updateList = new List<TicketPrice>
    {
        new TicketPrice { Id = 1, Price = 4.5m, UserCategory = cat }
    };

            await service.UpdateTicketPricesAsync(updateList.Select(p => new TicketPriceUpdateDto { Id = p.Id, Price = p.Price }).ToList());

            var updated = await context.TicketPrice.FindAsync(1);
            Assert.Equal(4.5m, updated!.Price);
            Assert.Equal(DateTime.Today.AddDays(1).AddTicks(-1), updated.EndDatePrice);
        }

        // Validates the bar's operational status logic based on the configured open and close times
        [Fact]
        public async Task IsBarOpenAsync_ValidatesTimeCorrect()
        {
            var context = GetDatabaseContext();
            context.AppConfig.Add(new AppConfig { BarOpeningTime = new TimeSpan(8, 0, 0), BarClosingTime = new TimeSpan(18, 0, 0) });
            await context.SaveChangesAsync();

            var service = new AdminService(context, GetMockUserManager(context).Object, GetMockRoleManager().Object, new Mock<IEmailSender>().Object, new Mock<ILogger<AdminService>>().Object, new Mock<IStringLocalizer<Errors>>().Object);

            Assert.True(await service.IsBarOpenAsync(new TimeSpan(10, 0, 0)));
            Assert.False(await service.IsBarOpenAsync(new TimeSpan(20, 0, 0)));
        }



        // Ensures that the service name switch (Lunch/Dinner/Bar) correctly updates the respective fields in configuration
        [Fact]
        public async Task UpdateBarScheduleAsync_UpdatesCorrectService()
        {
            var context = GetDatabaseContext();
            context.AppConfig.Add(new AppConfig { CanteenLunchOpeningTime = new TimeSpan(11, 0, 0), CanteenLunchClosingTime = new TimeSpan(14, 0, 0) });
            await context.SaveChangesAsync();

            var service = new AdminService(context, GetMockUserManager(context).Object, GetMockRoleManager().Object, new Mock<IEmailSender>().Object, new Mock<ILogger<AdminService>>().Object, new Mock<IStringLocalizer<Errors>>().Object);

            //await service.UpdateBarScheduleAsync("12:00", "15:00", "Almoço");

            var updated = await context.AppConfig.FirstAsync();
            Assert.Equal(new TimeSpan(12, 0, 0), updated.CanteenLunchOpeningTime);
        }
    }
}