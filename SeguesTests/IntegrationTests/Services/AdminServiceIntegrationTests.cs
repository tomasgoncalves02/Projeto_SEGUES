using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Localization;
using Microsoft.AspNetCore.Identity.UI.Services;
using Moq;
using Projeto_SEGUES.Areas.Admin.ViewModels;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models.Admin;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Resources;
using Projeto_SEGUES.Services;
using Xunit;

namespace SeguesTests.IntegrationTests.Services
{
    public class FakeEmailSender : EmailSender, IEmailSender
    {
        public FakeEmailSender() : base(null!) { }
        public new Task SendEmailAsync(string email, string subject, string htmlMessage) => Task.CompletedTask;
    }

    public class AdminServiceIntegrationTests
    {
        private (AppDbContext context, UserManager<AppUser> userManager, RoleManager<Role> roleManager) GetSetup()
        {
            var connection = new SqliteConnection("Filename=:memory:");
            connection.Open();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connection)
                .Options;

            var context = new AppDbContext(options);
            context.Database.EnsureCreated();

            var userStoreMock = new Mock<IUserStore<AppUser>>();
            var userRoleStoreMock = userStoreMock.As<IUserRoleStore<AppUser>>();
            var userEmailStoreMock = userStoreMock.As<IUserEmailStore<AppUser>>();
            var userPasswordStoreMock = userStoreMock.As<IUserPasswordStore<AppUser>>();
            var queryableStoreMock = userStoreMock.As<IQueryableUserStore<AppUser>>();

            queryableStoreMock.Setup(s => s.Users).Returns(context.Users);

            userEmailStoreMock.Setup(s => s.FindByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((string email, CancellationToken t) =>
                    context.Users.FirstOrDefault(u => u.Email!.ToUpper() == email.ToUpper()));

            userEmailStoreMock.Setup(s => s.SetNormalizedEmailAsync(It.IsAny<AppUser>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<AppUser, string, CancellationToken>((u, email, t) => u.NormalizedEmail = email)
                .Returns(Task.CompletedTask);

            userEmailStoreMock.Setup(s => s.GetNormalizedEmailAsync(It.IsAny<AppUser>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((AppUser u, CancellationToken t) => u.NormalizedEmail);

            userRoleStoreMock.Setup(s => s.IsInRoleAsync(It.IsAny<AppUser>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((AppUser u, string roleName, CancellationToken t) => {
                    var role = context.Roles.FirstOrDefault(r => r.Name == roleName);
                    return role != null && context.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == role.Id);
                });

            userRoleStoreMock.Setup(s => s.AddToRoleAsync(It.IsAny<AppUser>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<AppUser, string, CancellationToken>((user, roleName, token) => {
                    var role = context.Roles.FirstOrDefault(r => r.Name == roleName);
                    if (role != null)
                    {
                        context.UserRoles.Add(new IdentityUserRole<string> { UserId = user.Id, RoleId = role.Id });
                        context.SaveChanges();
                    }
                })
                .Returns(Task.CompletedTask);

            userPasswordStoreMock.Setup(s => s.SetPasswordHashAsync(It.IsAny<AppUser>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<AppUser, string, CancellationToken>((u, hash, t) => u.PasswordHash = hash)
                .Returns(Task.CompletedTask);

            userPasswordStoreMock.Setup(s => s.GetPasswordHashAsync(It.IsAny<AppUser>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((AppUser u, CancellationToken t) => u.PasswordHash);

            userRoleStoreMock.Setup(s => s.CreateAsync(It.IsAny<AppUser>(), It.IsAny<CancellationToken>()))
                .Callback<AppUser, CancellationToken>((user, token) => {
                    context.Users.Add(user);
                    context.SaveChanges();
                })
                .ReturnsAsync(IdentityResult.Success);

            userRoleStoreMock.Setup(s => s.GetUserIdAsync(It.IsAny<AppUser>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((AppUser u, CancellationToken t) => u.Id);

            userRoleStoreMock.Setup(s => s.GetUserNameAsync(It.IsAny<AppUser>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((AppUser u, CancellationToken t) => u.UserName);

            var userManager = new UserManager<AppUser>(userStoreMock.Object, null!, new PasswordHasher<AppUser>(), null!, null!, null!, null!, null!, null!);
            var roleStore = new RoleStore<Role>(context);
            var roleManager = new RoleManager<Role>(roleStore, null!, null!, null!, null!);

            return (context, userManager, roleManager);
        }

        [Fact]
        public async Task CreateInternalUserAsync_Success_PersistsUserAndAssignsRole()
        {
            var (context, userManager, roleManager) = GetSetup();

            await roleManager.CreateAsync(new Role { Name = "Admin", DisplayName = "Administrador" });
            context.UserCategory.Add(new UserCategory { Name = "Externo" });
            await context.SaveChangesAsync();

            var fakeEmail = new FakeEmailSender();
            var localizerMock = new Mock<IStringLocalizer<Errors>>();
            localizerMock.Setup(l => l[It.IsAny<string>()]).Returns(new LocalizedString("key", "value"));

            var service = new AdminService(context, userManager, roleManager,
                fakeEmail, Mock.Of<ILogger<AdminService>>(),
                localizerMock.Object, Mock.Of<IUserService>());

            var model = new CreateInternalUserViewModel
            {
                AccountType = "Admin",
                Email = "pedro@admin.pt",
                FirstName = "Pedro",
                LastName = "Integration",
                Gender = Gender.Male,
                BirthDate = DateTime.Now.AddYears(-30)
            };

            var result = await service.CreateInternalUserAsync(model);

            Assert.True(result.Success);
            var user = await userManager.FindByEmailAsync("pedro@admin.pt");
            Assert.NotNull(user);
            var isInRole = await userManager.IsInRoleAsync(user!, "Admin");
            Assert.True(isInRole);
        }

        [Fact]
        public async Task GetFilteredUsersAsync_ComplexFilter_ReturnsCorrectResults()
        {
            var (context, userManager, roleManager) = GetSetup();

            await roleManager.CreateAsync(new Role { Name = "Client", DisplayName = "Cliente" });
            var cat = new UserCategory { Name = "Student" };
            context.UserCategory.Add(cat);
            await context.SaveChangesAsync();

            var pedro = new AppUser
            {
                Id = "p1",
                UserName = "pedro@test.pt",
                Email = "pedro@test.pt",
                FirstName = "Pedro",
                LastName = "Silva",
                UserCategory = cat,
                Gender = Gender.Male,
                BirthDate = DateTime.Now.AddYears(-20)
            };

            var outro = new AppUser
            {
                Id = "u2",
                UserName = "maria@test.pt",
                Email = "maria@test.pt",
                FirstName = "Maria",
                LastName = "Santos",
                UserCategory = cat,
                Gender = Gender.Female,
                BirthDate = DateTime.Now.AddYears(-22)
            };

            await userManager.CreateAsync(pedro);
            await userManager.CreateAsync(outro);

            await userManager.AddToRoleAsync(pedro, "Client");
            await userManager.AddToRoleAsync(outro, "Client");

            var service = new AdminService(context, userManager, roleManager,
                Mock.Of<IEmailSender>(), Mock.Of<ILogger<AdminService>>(),
                Mock.Of<IStringLocalizer<Errors>>(), Mock.Of<IUserService>());

            var result = await service.GetFilteredUsersAsync("pedro", null, "Student");

            Assert.Single(result);
            Assert.Equal("Pedro Silva", result[0].FullName);
        }

        [Fact]
        public async Task UpdateScheduleAsync_PersistsChangesInAppConfig()
        {
            var (context, userManager, roleManager) = GetSetup();
            context.AppConfig.Add(new AppConfig
            {
                Id = 1,
                BarOpeningTime = new TimeSpan(8, 0, 0),
                BarClosingTime = new TimeSpan(20, 0, 0),
                CanteenLunchOpeningTime = new TimeSpan(11, 0, 0),
                CanteenLunchClosingTime = new TimeSpan(14, 0, 0),
                CanteenDinnerOpeningTime = new TimeSpan(18, 0, 0),
                CanteenDinnerClosingTime = new TimeSpan(21, 0, 0)
            });
            await context.SaveChangesAsync();

            var service = new AdminService(context, userManager, roleManager,
                Mock.Of<IEmailSender>(), Mock.Of<ILogger<AdminService>>(),
                Mock.Of<IStringLocalizer<Errors>>(), Mock.Of<IUserService>());

            var newSchedule = new BarCanteenConfigViewModel
            {
                BarOpeningTime = new TimeSpan(9, 0, 0),
                BarClosingTime = new TimeSpan(22, 0, 0)
            };

            await service.UpdateScheduleAsync(newSchedule);

            var config = await context.AppConfig.FirstAsync();
            Assert.Equal(new TimeSpan(9, 0, 0), config.BarOpeningTime);
            Assert.Equal(new TimeSpan(22, 0, 0), config.BarClosingTime);
        }
    }
}