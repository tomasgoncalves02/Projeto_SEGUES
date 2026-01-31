using Microsoft.AspNetCore.Identity;
using Projeto_SEGUES.Models;
using Projeto_SEGUES.Models.Enums; // Para aceder aos Enums
using static Projeto_SEGUES.Models.Enums.Enums;

namespace Projeto_SEGUES.Data
{
    public static class DbSeeder
    {
        public static async Task SeedRolesAndAdminAsync(IServiceProvider serviceProvider)
        {
            // Pede os serviços necessários ao sistema
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<User>>();

            // 1. CRIAR AS ROLES (Se não existirem)
            string[] rolesNames = { "Admin", "Employee", "Student", "External", "IPSWorker" };

            foreach (var roleName in rolesNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // 2. CRIAR O SUPER ADMIN (Se não existir)
            var adminEmail = "admin@admin.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                var newAdmin = new User
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FirstName = "Super",
                    LastName = "Admin",
                    Gender = Gender.Male, // Podes mudar se quiseres
                    EmailConfirmed = true, // Importante: Já vem confirmado!
                    Balance = 0m,
                    CreationDate = DateTime.Now,
                    Status = UserStatus.Active,
                    Role = UserRole.Admin
                };

                // Cria a conta com a password segura
                var createAdmin = await userManager.CreateAsync(newAdmin, "Admin123!");

                if (createAdmin.Succeeded)
                {
                    // Atribui a Role de Admin
                    await userManager.AddToRoleAsync(newAdmin, "Admin");
                }
            }
        }
    }
}