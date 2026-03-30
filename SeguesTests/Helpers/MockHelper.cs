using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Projeto_SEGUES.Areas.User.ViewModels;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;

namespace SeguesTests.Helpers;

public static class MockHelper
{
    #region Database & Identity Integration Setup
    
    public static (AppDbContext context, UserManager<AppUser> userManager, RoleManager<Role> roleManager) GetIdentitySetup()
    {
        // Database
        var connection = new SqliteConnection("Filename=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new AppDbContext(options);
        context.Database.EnsureCreated();

        // User
        var userStoreMock = new Mock<IUserStore<AppUser>>();
        SetupQueryableStore(userStoreMock, context);
        SetupEmailStore(userStoreMock, context);
        SetupRoleStore(userStoreMock, context);
        SetupPasswordStore(userStoreMock);
        
        // Create Managers
        var userManager = CreateUserManager(userStoreMock);
        var roleManager = CreateRoleManager(context);
        
        return (context, userManager, roleManager);
    }
    
    private static void SetupQueryableStore(Mock<IUserStore<AppUser>> baseMock, AppDbContext context)
    {
        var queryableMock = baseMock.As<IQueryableUserStore<AppUser>>();
        queryableMock.Setup(s => s.Users).Returns(context.Users);
    }
    
    private static void SetupEmailStore(Mock<IUserStore<AppUser>> baseMock, AppDbContext context)
    {
        var emailMock = baseMock.As<IUserEmailStore<AppUser>>();

        emailMock.Setup(s => s.FindByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string email, CancellationToken _) =>
                string.IsNullOrWhiteSpace(email) ? null : 
                    context.Users.FirstOrDefault(u => u.Email != null && u.Email.ToUpper() == email.ToUpper()));

        emailMock.Setup(s => s.SetNormalizedEmailAsync(It.IsAny<AppUser>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<AppUser, string, CancellationToken>((u, email, _) => u.NormalizedEmail = email)
            .Returns(Task.CompletedTask);

        emailMock.Setup(s => s.GetNormalizedEmailAsync(It.IsAny<AppUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AppUser u, CancellationToken _) => u.NormalizedEmail);
    }
    
    private static void SetupRoleStore(Mock<IUserStore<AppUser>> baseMock, AppDbContext context)
    {
        var roleMock = baseMock.As<IUserRoleStore<AppUser>>();

        roleMock.Setup(s => s.IsInRoleAsync(It.IsAny<AppUser>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AppUser u, string roleName, CancellationToken _) =>
            {
                var role = context.Roles.FirstOrDefault(r => r.Name == roleName);
                return role != null && context.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == role.Id);
            });
        
        roleMock.Setup(s => s.AddToRoleAsync(It.IsAny<AppUser>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<AppUser, string, CancellationToken>((user, roleName, _) =>
            {
                var role = context.Roles.FirstOrDefault(r => r.Name == roleName);
                if (role == null) return;
                context.UserRoles.Add(new IdentityUserRole<string> { UserId = user.Id, RoleId = role.Id });
                context.SaveChanges();
            })
            .Returns(Task.CompletedTask);
        
        roleMock.Setup(s => s.CreateAsync(It.IsAny<AppUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AppUser user, CancellationToken _) =>
            {
                if (string.IsNullOrWhiteSpace(user.Email)) return IdentityResult.Failed();
                var exists = context.Users.Any(u => u.Email != null && user.Email != null && u.Email.ToUpper() == user.Email.ToUpper());
                if (exists)
                {
                    return IdentityResult.Failed(new IdentityError
                    {
                        Code = "DuplicateEmail",
                        Description = $"Email '{user.Email}' já está em uso."
                    });
                }

                context.Users.Add(user);
                context.SaveChanges();
                return IdentityResult.Success;
            });
        
        roleMock.Setup(s => s.GetUserIdAsync(It.IsAny<AppUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AppUser u, CancellationToken _) => u.Id);
            
        roleMock.Setup(s => s.GetUserNameAsync(It.IsAny<AppUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AppUser u, CancellationToken _) => u.UserName);
    }
    
    private static void SetupPasswordStore(Mock<IUserStore<AppUser>> baseMock)
    {
        var passwordMock = baseMock.As<IUserPasswordStore<AppUser>>();
        passwordMock.Setup(s => s.SetPasswordHashAsync(It.IsAny<AppUser>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<AppUser, string, CancellationToken>((u, h, _) => u.PasswordHash = h)
            .Returns(Task.CompletedTask);
    }
    
    // Creates a real UserManager instance with the mock store.
    private static UserManager<AppUser> CreateUserManager(Mock<IUserStore<AppUser>> userStoreMock)
    {
        return new UserManager<AppUser>(
            userStoreMock.Object,
            new Mock<IOptions<IdentityOptions>>().Object,
            new PasswordHasher<AppUser>(),
            [new UserValidator<AppUser>()],
            [new PasswordValidator<AppUser>()],
            null!,
            new IdentityErrorDescriber(),
            new Mock<IServiceProvider>().Object,
            new Mock<ILogger<UserManager<AppUser>>>().Object
        );
    }
    
    // Creates a real RoleManager instance with the mock store.
    private static RoleManager<Role> CreateRoleManager(AppDbContext context)
    {
        var roleStore = new RoleStore<Role>(context);
        return new RoleManager<Role>(
            roleStore,
            [],
            null!,
            new IdentityErrorDescriber(),
            new Mock<ILogger<RoleManager<Role>>>().Object
        );
    }
    
    #endregion
    
    #region Mock Managers (Strict Unit Testing)
    
    public static Mock<UserManager<TUser>> MockUserManager<TUser>(List<TUser> ls) where TUser : AppUser
    {
        var store = new Mock<IUserStore<TUser>>();
        var mgr = new Mock<UserManager<TUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        
        mgr.Object.UserValidators.Add(new UserValidator<TUser>());
        mgr.Object.PasswordValidators.Add(new PasswordValidator<TUser>());

        mgr.Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((string email) => ls.Find(u => u.Email == email));
        mgr.Setup(x => x.CreateAsync(It.IsAny<TUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        mgr.Setup(x => x.AddToRoleAsync(It.IsAny<TUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        mgr.Setup(x => x.UpdateAsync(It.IsAny<TUser>()))
            .ReturnsAsync(IdentityResult.Success);
        mgr.Setup(x => x.GetRolesAsync(It.IsAny<TUser>()))
            .ReturnsAsync([]);
        
        return mgr;
    }
    
    public static Mock<RoleManager<TRole>> MockRoleManager<TRole>() where TRole : class
    {
        var store = new Mock<IRoleStore<TRole>>();
        var mgr = new Mock<RoleManager<TRole>>(store.Object, null!, null!, null!, null!);
        mgr.Setup(x => x.RoleExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(true);
        return mgr;
    }
    
    #endregion
    
    #region Controller Setup Fakes
    
    public static void SetupControllerContext(Controller controller, string userName = "Pedro", string userId = "pedro-77")
    {
        var claimsUser = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.Name, userName),
            new Claim(ClaimTypes.NameIdentifier, userId)
        ], "mock"));

        var httpContext = new DefaultHttpContext { User = claimsUser };

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
    }
    
    #endregion

    #region Entity Factories
    
    public static Student CreateValidStudent(string id = "pedro-77")
    {
        return new Student
        {
            Id = id,
            UserName = "Pedro",
            FirstName = "Pedro",
            LastName = "Jesus",
            Email = "pedro@segues.pt",
            BirthDate = new DateTime(2000, 1, 1),
            Gender = Gender.Male,
            UserCategory = new UserCategory { Name = "Student" },
            StudentNumber = "12345",
            School = new School
            {
                Id = 1,
                Name = "IPS",
                Code = "IPS",
                Address = "IPS",
                City = "Noruega"
            }
        };
    }
    
    public static AppUser CreateValidAppUser(string id = "pedro-77")
    {
        return new AppUser
        {
            Id = id,
            UserName = "Pedro",
            FirstName = "Pedro",
            LastName = "Jesus",
            Email = "pedro@segues.pt",
            BirthDate = new DateTime(2000, 1, 1),
            Gender = Gender.Male,
            UserCategory = new UserCategory { Name = "Cliente" }
        };
    }
    
    public static Role CreateValidRole(string name = "Student", string displayName = "Estudante")
    {
        return new Role
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            DisplayName = displayName
        };
    }
    
    public static EditUserViewModel CreateValidEditUserViewModel(
        string id = "pedro-77",
        string lastName = "Jesus",
        string email = "pedro@segues.pt",
        string category = "Cliente")
    {
        return new EditUserViewModel
        {
            Id = id,
            FirstName = "Pedro",
            LastName = lastName,
            Email = email,
            BirthDate = new DateTime(2000, 1, 1),
            Gender = Gender.Male,
            Category = category,
            Role = CreateValidRole()
        };
    }
    
    #endregion
    
    #region Fakes & Stubs
    
    public class FakeEmailSender() : EmailSender(null!), IEmailSender
    {
        public new Task SendEmailAsync(string email, string subject, string htmlMessage) => Task.CompletedTask;
    }
    
    #endregion
}