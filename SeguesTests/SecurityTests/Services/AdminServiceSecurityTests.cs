using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Moq;
using Projeto_SEGUES.Areas.Admin.ViewModels;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Resources;
using Projeto_SEGUES.Services;

namespace SeguesTests.SecurityTests.Services;

public class AdminServiceSecurityTests
{
    private static AppDbContext GetContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new AppDbContext(options);
    }

    private static Mock<UserManager<AppUser>> GetMockUserManager()
    {
        var store = new Mock<IUserStore<AppUser>>();
        return new Mock<UserManager<AppUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }

    private static Mock<RoleManager<Role>> GetMockRoleManager()
    {
        var store = new Mock<IRoleStore<Role>>();
        return new Mock<RoleManager<Role>>(store.Object, null!, null!, null!, null!);
    }

    [Fact]
    public async Task CreateInternalUserAsync_InvalidRole_ReturnsFailure()
    {
        var context = GetContext();
        var roleManager = GetMockRoleManager();
        roleManager.Setup(r => r.FindByNameAsync("HackerRole")).ReturnsAsync((Role)null!);
        
        var service = new AdminService(
            context,
            GetMockUserManager().Object,
            roleManager.Object,
            Mock.Of<IEmailSender>(),
            Mock.Of<ILogger<AdminService>>(),
            Mock.Of<IUserService>());

        var model = new CreateInternalUserViewModel
        {
            AccountType = "HackerRole",
            Email = "pedro@test.pt",
            FirstName = "Pedro",
            LastName = "Security",
            Gender = Gender.Male,
            BirthDate = DateTime.Now.AddYears(-20)
        };

        var result = await service.CreateInternalUserAsync(model);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task UpdateUserAdminAsync_DuplicateEmail_ReturnsFailure()
    {
        var context = GetContext();
        var userManager = GetMockUserManager();

        var cat = new UserCategory { Name = "X" };
        var pedro = new AppUser { Id = "u1", Email = "pedro@original.pt", FirstName = "P", LastName = "S", BirthDate = DateTime.Now, Gender = Gender.Male, UserCategory = cat };
        var outro = new AppUser { Id = "u2", Email = "ja-existe@test.pt", FirstName = "O", LastName = "U", BirthDate = DateTime.Now, Gender = Gender.Male, UserCategory = cat };

        userManager.Setup(u => u.FindByEmailAsync("ja-existe@test.pt")).ReturnsAsync(outro);

        var service = new AdminService(context, userManager.Object, GetMockRoleManager().Object,
            Mock.Of<IEmailSender>(), Mock.Of<ILogger<AdminService>>(), Mock.Of<IUserService>());

        var model = new EditUserAdminViewModel
        {
            Id = "u1",
            Email = "ja-existe@test.pt",
            FirstName = "Pedro",
            LastName = "S",
            Gender = Gender.Male,
            BirthDate = DateTime.Now.AddYears(-20),
            Category = "Student",
            Role = "Client"
        };

        var result = await service.UpdateUserAdminAsync(pedro, model, null!, "http");

        Assert.False(result.Success);
        Assert.Equal("Este email já está em uso.", result.Message);
    }
}