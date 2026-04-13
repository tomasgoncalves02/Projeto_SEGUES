using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Localization;
using Microsoft.AspNetCore.Identity.UI.Services;
using Moq;
using Projeto_SEGUES.Areas.Admin.ViewModels;
using Projeto_SEGUES.Models.Admin;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Resources;
using Projeto_SEGUES.Services;
using SeguesTests.Helpers;

namespace SeguesTests.IntegrationTests.Services;

public class FakeEmailSender() : EmailSender(null!), IEmailSender
{
    public new Task SendEmailAsync(string email, string subject, string htmlMessage) => Task.CompletedTask;
}

public class AdminServiceIntegrationTests
{
    
    [Fact]
    public async Task CreateInternalUserAsync_Success_PersistsUserAndAssignsRole()
    {
        var (context, userManager, roleManager) = MockHelper.GetIdentitySetup();

        await roleManager.CreateAsync(new Role { Name = "Admin", DisplayName = "Administrador" });
        context.UserCategory.Add(new UserCategory { Name = "Externo" });
        await context.SaveChangesAsync();

        var fakeEmail = new FakeEmailSender();
        
        var service = new AdminService(context, userManager, roleManager,
            fakeEmail, Mock.Of<ILogger<AdminService>>(), Mock.Of<IUserService>());

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
        var isInRole = await userManager.IsInRoleAsync(user, "Admin");
        Assert.True(isInRole);
    }

    [Fact]
    public async Task GetFilteredUsersAsync_ComplexFilter_ReturnsCorrectResults()
    {
        var (context, userManager, roleManager) = MockHelper.GetIdentitySetup();

        await roleManager.CreateAsync(new Role { Id = Guid.NewGuid().ToString(), Name = "Client", DisplayName = "Cliente" });
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

        await userManager.CreateAsync(pedro);
        await userManager.AddToRoleAsync(pedro, "Client");

        var service = new AdminService(context, userManager, roleManager,
            Mock.Of<IEmailSender>(), Mock.Of<ILogger<AdminService>>(), Mock.Of<IUserService>());

        var result = await service.GetFilteredUsersAsync(new UserSearchViewModel { SearchString = "pedro", RoleFilter = null, CategoryFilter = "Student" });

        Assert.Single(result);
        Assert.Equal("Pedro Silva", result[0].FullName);
    }

    [Fact]
    public async Task UpdateScheduleAsync_PersistsChangesInAppConfig()
    {
        var (context, userManager, roleManager) = MockHelper.GetIdentitySetup();
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
            Mock.Of<IEmailSender>(), Mock.Of<ILogger<AdminService>>(), Mock.Of<IUserService>());

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