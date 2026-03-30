using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Moq;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.Ticket;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Resources;
using Projeto_SEGUES.Services;

namespace SeguesTests.RegressionTests.Services;

public class FakeEmailSender() : EmailSender(null!), IEmailSender
{
    public new Task SendEmailAsync(string e, string s, string h) => Task.CompletedTask;
}

public class AdminServiceRegressionTests
{
    private static (AppDbContext context, UserManager<AppUser> userManager, RoleManager<Role> roleManager) GetSetup()
    {
        var connection = new SqliteConnection("Filename=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        var context = new AppDbContext(options);
        context.Database.EnsureCreated();

        var userStoreMock = new Mock<IUserStore<AppUser>>();
        var userRoleStoreMock = userStoreMock.As<IUserRoleStore<AppUser>>();
        var queryableStoreMock = userStoreMock.As<IQueryableUserStore<AppUser>>();

        queryableStoreMock.Setup(s => s.Users).Returns(context.Users);

        userRoleStoreMock.Setup(s => s.GetUserIdAsync(It.IsAny<AppUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AppUser u, CancellationToken _) => u.Id);

        var userManager = new UserManager<AppUser>(userStoreMock.Object, null!, new PasswordHasher<AppUser>(), null!, null!, null!, null!, null!, null!);
        var roleStore = new RoleStore<Role>(context);
        var roleManager = new RoleManager<Role>(roleStore, null!, null!, null!, null!);

        return (context, userManager, roleManager);
    }

    [Theory]
    [InlineData("PEDRO", null, null, 1)]
    [InlineData("silva", null, null, 1)]
    [InlineData(null, "Admin", null, 1)]
    [InlineData(null, null, "Externo", 2)]
    [InlineData("Pedro", "Admin", "Externo", 1)]
    [InlineData("Inexistente", null, null, 0)]
    public async Task GetFilteredUsersAsync_Regression_SearchCombinations(string search, string role, string cat, int expectedCount)
    {
        var (context, userManager, roleManager) = GetSetup();

        var roleAdmin = new Role { Id = "r1", Name = "Admin", DisplayName = "Adm" };
        var roleUser = new Role { Id = "r2", Name = "Client", DisplayName = "Cli" };
        await roleManager.CreateAsync(roleAdmin);
        await roleManager.CreateAsync(roleUser);

        var catExt = new UserCategory { Id = 1, Name = "Externo" };
        context.UserCategory.Add(catExt);

        var user1 = new AppUser { Id = "u1", UserName = "p@t.pt", Email = "p@t.pt", FirstName = "Pedro", LastName = "Silva", UserCategory = catExt, BirthDate = DateTime.Now, Gender = Gender.Male };
        var user2 = new AppUser { Id = "u2", UserName = "j@t.pt", Email = "j@t.pt", FirstName = "Joao", LastName = "Santos", UserCategory = catExt, BirthDate = DateTime.Now, Gender = Gender.Male };

        context.Users.AddRange(user1, user2);
        context.UserRoles.Add(new IdentityUserRole<string> { UserId = "u1", RoleId = "r1" });
        context.UserRoles.Add(new IdentityUserRole<string> { UserId = "u2", RoleId = "r2" });
        await context.SaveChangesAsync();

        var service = new AdminService(context, userManager, roleManager, new FakeEmailSender(), Mock.Of<ILogger<AdminService>>(), Mock.Of<IStringLocalizer<Errors>>(), Mock.Of<IUserService>());

        var result = await service.GetFilteredUsersAsync(search, role, cat);

        Assert.Equal(expectedCount, result.Count);
    }

    [Fact]
    public async Task GetTicketPricesAsync_Regression_ReturnsOnlyActivePrices()
    {
        var (context, userManager, roleManager) = GetSetup();
        var cat = new UserCategory { Id = 1, Name = "Pedro-Student" };

        context.TicketPrice.AddRange(
            new TicketPrice { Id = 1, UserCategory = cat, Price = 1.0m, EndDatePrice = DateTime.Today.AddDays(-1), InitialDatePrice = DateTime.Today.AddDays(-10) },
            new TicketPrice { Id = 2, UserCategory = cat, Price = 2.5m, InitialDatePrice = DateTime.Today, EndDatePrice = null }
        );
        await context.SaveChangesAsync();

        var service = new AdminService(context, userManager, roleManager, new FakeEmailSender(), Mock.Of<ILogger<AdminService>>(), Mock.Of<IStringLocalizer<Errors>>(), Mock.Of<IUserService>());

        var result = await service.GetTicketPricesAsync();

        Assert.Single(result);
        Assert.Equal(2.5m, result[0].Price);
    }
}