using Microsoft.AspNetCore.Identity;
using Moq;
using Projeto_SEGUES.Areas.User.ViewModels;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.User;

namespace SeguesTests.Helpers
{
    public static class MockHelper
    {
        public static Mock<UserManager<TUser>> MockUserManager<TUser>(List<TUser> ls) where TUser : class
        {
            var store = new Mock<IUserStore<TUser>>();
            var mgr = new Mock<UserManager<TUser>>(store.Object, null, null, null, null, null, null, null, null);

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
            var mgr = new Mock<RoleManager<TRole>>(store.Object, null, null, null, null);

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
    }
}