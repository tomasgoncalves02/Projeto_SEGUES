using Microsoft.AspNetCore.Identity;
using Moq;
using System.Collections.Generic;

namespace Projeto_SEGUES.Tests.Helpers
{
    public static class MockHelper
    {
        // Simula o UserManager
        public static Mock<UserManager<TUser>> MockUserManager<TUser>(List<TUser> ls) where TUser : class
        {
            var store = new Mock<IUserStore<TUser>>();
            var mgr = new Mock<UserManager<TUser>>(store.Object, null, null, null, null, null, null, null, null);

            mgr.Object.UserValidators.Add(new UserValidator<TUser>());
            mgr.Object.PasswordValidators.Add(new PasswordValidator<TUser>());

            // Configuração para simular que encontrou utilizador por email
            mgr.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((string email) => ls.Find(u => (u as dynamic).Email == email));

            // Configuração para simular criação de utilizador (sempre sucesso)
            mgr.Setup(x => x.CreateAsync(It.IsAny<TUser>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);
            mgr.Setup(x => x.AddToRoleAsync(It.IsAny<TUser>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);
            mgr.Setup(x => x.UpdateAsync(It.IsAny<TUser>())).ReturnsAsync(IdentityResult.Success);

            return mgr;
        }

        // Simula o RoleManager
        public static Mock<RoleManager<TRole>> MockRoleManager<TRole>() where TRole : class
        {
            var store = new Mock<IRoleStore<TRole>>();
            var mgr = new Mock<RoleManager<TRole>>(store.Object, null, null, null, null);

            // Diz sempre que a role existe (para não dar erro)
            mgr.Setup(x => x.RoleExistsAsync(It.IsAny<string>())).ReturnsAsync(true);

            return mgr;
        }
    }
}