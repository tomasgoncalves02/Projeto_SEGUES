using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using Projeto_SEGUES.Areas.User.ViewModels;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;

namespace SeguesTests.Helpers
{
    public static class MockHelper
    {
        public static (AppDbContext context, UserManager<AppUser> userManager, RoleManager<Role> roleManager) GetIdentitySetup()
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

            userRoleStoreMock.Setup(s => s.CreateAsync(It.IsAny<AppUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AppUser user, CancellationToken token) =>
            {
                var exists = context.Users.Any(u => u.Email!.ToUpper() == user.Email!.ToUpper());
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

            userPasswordStoreMock.Setup(s => s.SetPasswordHashAsync(It.IsAny<AppUser>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<AppUser, string, CancellationToken>((u, h, t) => u.PasswordHash = h)
                .Returns(Task.CompletedTask);

            userRoleStoreMock.Setup(s => s.GetUserIdAsync(It.IsAny<AppUser>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((AppUser u, CancellationToken t) => u.Id);
            userRoleStoreMock.Setup(s => s.GetUserNameAsync(It.IsAny<AppUser>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((AppUser u, CancellationToken t) => u.UserName);

            var userManager = new UserManager<AppUser>(userStoreMock.Object, null!, new PasswordHasher<AppUser>(), null!, null!, null!, null!, null!, null!);

            var roleStore = new RoleStore<Role>(context);
            var roleManager = new RoleManager<Role>(roleStore, null!, null!, null!, null!);

            return (context, userManager, roleManager);
        }

        public static Mock<UserManager<TUser>> MockUserManager<TUser>(List<TUser> ls) where TUser : class
        {
            var store = new Mock<IUserStore<TUser>>();
            var mgr = new Mock<UserManager<TUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

            mgr.Object.UserValidators.Add(new UserValidator<TUser>());
            mgr.Object.PasswordValidators.Add(new PasswordValidator<TUser>());

            mgr.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((string email) => ls.Find(u => (u as dynamic).Email == email));
            mgr.Setup(x => x.CreateAsync(It.IsAny<TUser>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);
            mgr.Setup(x => x.AddToRoleAsync(It.IsAny<TUser>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);
            mgr.Setup(x => x.UpdateAsync(It.IsAny<TUser>())).ReturnsAsync(IdentityResult.Success);
            mgr.Setup(x => x.GetRolesAsync(It.IsAny<TUser>())).ReturnsAsync(new List<string>());

            return mgr;
        }

        public static Mock<RoleManager<TRole>> MockRoleManager<TRole>() where TRole : class
        {
            var store = new Mock<IRoleStore<TRole>>();
            var mgr = new Mock<RoleManager<TRole>>(store.Object, null!, null!, null!, null!);
            mgr.Setup(x => x.RoleExistsAsync(It.IsAny<string>())).ReturnsAsync(true);
            return mgr;
        }

        public static Student CreateValidStudent()
        {
            return new Student
            {
                Id = "pedro-77",
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

        public class FakeEmailSender : EmailSender, IEmailSender
        {
            public FakeEmailSender() : base(null!) { }
            public new Task SendEmailAsync(string email, string subject, string htmlMessage) => Task.CompletedTask;
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
    }
}