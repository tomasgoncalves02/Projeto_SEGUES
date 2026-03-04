using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Projeto_SEGUES.Models.Admin;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.Inventory;
using Projeto_SEGUES.Models.Ticket;
using Projeto_SEGUES.Models.User;

namespace Projeto_SEGUES.Data
{
    public static class DbSeeder
    {
        public static async Task SeedRolesAndAdminAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<Role>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<AppUser>>();
            var context = serviceProvider.GetRequiredService<AppDbContext>();
            
            // AppConfig
            if (!await context.AppConfigs.AnyAsync())
            {
                var appConfig = new AppConfig();
                await context.AppConfigs.AddAsync(appConfig);
            }
            
            // Roles
            var roles = new[]
            {
                new Role { Name = "Admin", DisplayName = "Administrator" },
                new Role { Name = "Employee", DisplayName = "Funcionário" },
                new Role { Name = "Client", DisplayName = "Cliente" }
            };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role.Name!))
                {
                    await roleManager.CreateAsync(role);
                }
            }
            
            // UserCategories
            var categories = new[]
            {
                new UserCategory { Name = "Estudante"},
                new UserCategory { Name = "Externo"},
                new UserCategory { Name = "Trabalhador IPS"}
            };

            foreach (var category in categories)
            {
                if (!await context.UserCategories.AnyAsync(c => c.Name == category.Name))
                    await context.UserCategories.AddAsync(category);
            }
            await context.SaveChangesAsync();
            
            // TicketPrice
            var defaultPrices = new Dictionary<string, decimal>
            {
                { "Estudante", 2.90m },
                { "Externo", 5.50m },
                { "Trabalhador IPS", 5.20m }
            };

            foreach (var (category, price) in defaultPrices)
            {
                // Get cat for FK
                var catDb = await context.UserCategories
                    .FirstOrDefaultAsync(c => c.Name == category);
                if (catDb == null) continue;
                
                if (await context.TicketPrices.AnyAsync(tp => tp.UserCategory.Id == catDb.Id && tp.EndDatePrice > DateTime.Now))
                    continue;
                
                context.TicketPrices.Add(new TicketPrice
                {
                    UserCategory = catDb,
                    Price = price,
                    InitialDatePrice = DateTime.Now,
                    EndDatePrice = DateTime.Now.AddYears(1)
                });
            }
            await context.SaveChangesAsync();

            // Create Admin
            var adminEmail = "admin@admin.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            var adminCategory = await context.UserCategories.FirstOrDefaultAsync(uc => uc.Name == "Externo");

            if (adminUser == null)
            {
                var newAdmin = new AppUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FirstName = "Super",
                    LastName = "Admin",
                    Gender = Gender.Male,
                    BirthDate = new DateTime(1980, 1, 1),
                    EmailConfirmed = true,
                    Balance = 1000m,
                    CreationDate = DateTime.Now,
                    Status = UserStatus.Active,
                    UserCategory = adminCategory!
                };
                
                var createAdmin = await userManager.CreateAsync(newAdmin, "AdminSEGUES123!");

                if (createAdmin.Succeeded)
                {
                    await userManager.AddToRoleAsync(newAdmin, "Admin");
                }
            }
            await context.SaveChangesAsync();
            
            // ProductCategory
            var defaultProductsCategories = new List<ProductCategory>
            {
                new ProductCategory { Name = "Bebidas", Description = "Café, refrigerantes e outras bebidas." },
                new ProductCategory { Name = "Refeições", Description = "Sanduíches, saladas e refeições rápidas." },
                new ProductCategory { Name = "Doces", Description = "Bolos, tortas e outras sobremesas." },
                new ProductCategory { Name = "Salgados", Description = "Empadas, croissants e outros salgados." }
            };
            foreach (var category in defaultProductsCategories)
            {
                if (!await context.ProductCategories.AnyAsync(c => c.Name == category.Name))
                    await context.ProductCategories.AddAsync(category);
            }
            await context.SaveChangesAsync();
            
            // Products
            var dbProductCategories = await context.ProductCategories.ToListAsync();
            var defaultProducts = new List<Product>
            {
                new Product
                {
                    Name = "Folhado Misto",
                    Description = "Folhado com fiambre e queijo. Contém glúten e lactose.",
                    Category = dbProductCategories.FirstOrDefault(c => c.Name == "Salgados")!,
                    Price = 1.80m,
                    Stock = 20,
                    MinimumStock = 5,
                    IsActive = true
                },
                new Product
                {
                    Name = "Café Expresso",
                    Description = "Café simples de máquina",
                    Category = dbProductCategories.FirstOrDefault(c => c.Name == "Bebidas")!,
                    Price = 0.70m,
                    Stock = 100,
                    MinimumStock = 10,
                    IsActive = true
                },
                new Product
                {
                    Name = "Água Mineral 0.5L",
                    Description = "Água mineral sem gás",
                    Category = dbProductCategories.FirstOrDefault(c => c.Name == "Bebidas")!,
                    Price = 1.00m,
                    Stock = 50,
                    MinimumStock = 10,
                    IsActive = true
                },
                new Product
                {
                    Name = "Tosta Mista",
                    Description = "Tosta com fiambre e queijo. Contém glúten e lactose.",
                    Category = dbProductCategories.FirstOrDefault(c => c.Name == "Refeições")!,
                    Price = 2.50m,
                    Stock = 30,
                    MinimumStock = 5,
                    IsActive = true
                }
            };

            foreach (var product in defaultProducts)
            {
                if (!await context.Products.AnyAsync(p => p.Name == product.Name))
                    await context.Products.AddAsync(product);
            }
            await context.SaveChangesAsync();
        }
    }
}