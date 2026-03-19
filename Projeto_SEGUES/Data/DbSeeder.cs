using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Projeto_SEGUES.Models.Admin;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.Inventory;
using Projeto_SEGUES.Models.Order;
using Projeto_SEGUES.Models.Ticket;
using Projeto_SEGUES.Models.User;

namespace Projeto_SEGUES.Data
{
    public static class DbSeeder
    {
        public static async Task SeedInitialDataAsync(IServiceProvider serviceProvider)
        {
            await SeedRolesAndAdminAsync(serviceProvider);
            await SeedInventoryAsync(serviceProvider);
            await SeedTestData(serviceProvider); // TODO: Remove on app release
        }

        public static async Task SeedRolesAndAdminAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<Role>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<AppUser>>();
            var context = serviceProvider.GetRequiredService<AppDbContext>();

            // AppConfig
            if (!await context.AppConfig.AnyAsync())
            {
                var appConfig = new AppConfig();
                // Links for current IPS menus
                appConfig.BarLink = "https://software.movelife.net/pt-PT/Menus/PublicCC/Tj6o3O_vCFDXvHU0nbgTmg%3d%3d";
                appConfig.CanteenLink = "https://software.movelife.net/pt-PT/Menus/PublicCC/Tj6o3O_vCFB2LmCmm9VUjw%3d%3d";
                await context.AppConfig.AddAsync(appConfig);
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
                if (!await context.UserCategory.AnyAsync(c => c.Name == category.Name))
                    await context.UserCategory.AddAsync(category);
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
                var catDb = await context.UserCategory
                    .FirstOrDefaultAsync(c => c.Name == category);
                if (catDb == null) continue;

                if (await context.TicketPrice.AnyAsync(tp => tp.UserCategory.Id == catDb.Id && tp.EndDatePrice > DateTime.Now))
                    continue;

                context.TicketPrice.Add(new TicketPrice
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
            var adminCategory = await context.UserCategory.FirstOrDefaultAsync(uc => uc.Name == "Externo");

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
        }

        public static async Task SeedInventoryAsync(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<AppDbContext>();

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
                if (!await context.ProductCategory.AnyAsync(c => c.Name == category.Name))
                    await context.ProductCategory.AddAsync(category);
            }
            await context.SaveChangesAsync();
        }

        public static async Task SeedTestData(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<AppDbContext>();
            var userManager = serviceProvider.GetRequiredService<UserManager<AppUser>>();

            // Create employee
            var employeeEmail = "employee@employee.com";
            var employeeUser = await userManager.FindByEmailAsync(employeeEmail);
            if (employeeUser == null)
            {
                var employeeCategory = await context.UserCategory.FirstOrDefaultAsync(uc => uc.Name == "Externo");
                var newEmployee = new AppUser
                {
                    UserName = employeeEmail,
                    Email = employeeEmail,
                    FirstName = "Major",
                    LastName = "Employee",
                    Gender = Gender.Male,
                    BirthDate = new DateTime(1980, 1, 1),
                    EmailConfirmed = true,
                    Balance = 1000m,
                    CreationDate = DateTime.Now,
                    Status = UserStatus.Active,
                    UserCategory = employeeCategory!
                };
                var createEmployee = await userManager.CreateAsync(newEmployee, "AdminSEGUES123!");

                if (createEmployee.Succeeded)
                {
                    await userManager.AddToRoleAsync(newEmployee, "Employee");
                }
            }
            await context.SaveChangesAsync();

            // Products default data
            var dbProductCategories = await context.ProductCategory.ToListAsync();
            var defaultProducts = new List<Product>
            {
                new()
                {
                    Name = "Folhado Misto",
                    Description = "Folhado com fiambre e queijo. Contém glúten e lactose.",
                    Category = dbProductCategories.FirstOrDefault(c => c.Name == "Salgados")!,
                    Price = 1.80m,
                    Stock = 20,
                    MinimumStock = 5,
                    IsActive = true
                },
                new()
                {
                    Name = "Café Expresso",
                    Description = "Café simples de máquina",
                    Category = dbProductCategories.FirstOrDefault(c => c.Name == "Bebidas")!,
                    Price = 0.70m,
                    Stock = 100,
                    MinimumStock = 10,
                    IsActive = true
                },
                new()
                {
                    Name = "Água Mineral 0.5L",
                    Description = "Água mineral sem gás",
                    Category = dbProductCategories.FirstOrDefault(c => c.Name == "Bebidas")!,
                    Price = 1.00m,
                    Stock = 50,
                    MinimumStock = 10,
                    IsActive = true
                },
                new()
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
                if (!await context.Product.AnyAsync(p => p.Name == product.Name))
                    await context.Product.AddAsync(product);
            }
            await context.SaveChangesAsync();

            // Create tickets
            var adminUser = await userManager.FindByEmailAsync("admin@admin.com");
            employeeUser = await userManager.FindByEmailAsync(employeeEmail);
            var ticketPurchase = new TicketPurchase
            {
                Quantity = 40,
                TransactionDate = DateTime.Now.AddDays(-300),
                Value = 220m,
                AppUser = employeeUser!
            };
            if (!await context.TicketPurchase.AnyAsync(tp => tp.AppUser.Id == employeeUser!.Id))
            {
                await context.TicketPurchase.AddAsync(ticketPurchase);
            }
            await context.SaveChangesAsync();

            var tickets = new List<Ticket>();
            var now = DateTime.Now;

            /* 10 tickets same day different hours */
            var hourRange = Math.Max(1, now.Hour - 8 + 1);
            for (int i = 0; i < 10; i++)
            {
                var hour = 8 + (i % hourRange); // Start at 8h, end at current hour, loop if more than available hours
                var usedDate = now.Date.AddHours(hour); // 08h–17h but no future time
                tickets.Add(new Ticket
                {
                    ExpirationDate = now.AddDays(365),
                    State = TicketState.Used,
                    IsUsed = true,
                    UsedDate = usedDate,
                    Owner = employeeUser!,
                    ValidationCode = Guid.NewGuid().ToString("N")[..8].ToUpper(),
                    ValidatedBy = adminUser,
                    TicketPurchase = ticketPurchase
                });
            }
            /* 10 tickets in the same week but different days */
            var startOfWeek = now.Date.AddDays(-(int)now.DayOfWeek + (int)DayOfWeek.Sunday);
            var dayOfWeekRange = Math.Max(1, (now.Date - startOfWeek).Days + 1);
            for (int i = 0; i < 10; i++)
            {
                var day = (i % dayOfWeekRange) + 1; // Start at Sunday, end at current day, loop if more than available days
                var usedDate = startOfWeek.AddDays(day).AddHours(8 + (i % 8));
                if (usedDate > now) usedDate = usedDate.Date.AddHours(8 + (i % hourRange)); // If future time, set to current day with hour loop
                tickets.Add(new Ticket
                {
                    ExpirationDate = now.AddDays(365),
                    State = TicketState.Used,
                    IsUsed = true,
                    UsedDate = usedDate,
                    Owner = employeeUser!,
                    ValidationCode = Guid.NewGuid().ToString("N")[..8].ToUpper(),
                    ValidatedBy = adminUser,
                    TicketPurchase = ticketPurchase
                });
            }
            /* 10 tickets in the same month spread out by days */
            var startOfMonth = new DateTime(now.Year, now.Month, 1);
            var dayOfMonthRange = Math.Max(1, (now.Date - startOfMonth).Days + 1);
            for (int i = 0; i < 10; i++)
            {
                var day = ((i * 3) % dayOfMonthRange) + 1; // Start at 1st, end at current day, loop if more than available days
                var usedDate = startOfMonth.AddDays(day).AddHours(8 + (i % 8));
                if (usedDate > now) usedDate = usedDate.Date.AddHours(8 + (i % hourRange)); // If future time, set to current day with hour loop
                tickets.Add(new Ticket
                {
                    ExpirationDate = now.AddDays(365),
                    State = TicketState.Used,
                    IsUsed = true,
                    UsedDate = usedDate,
                    Owner = employeeUser!,
                    ValidationCode = Guid.NewGuid().ToString("N")[..8].ToUpper(),
                    ValidatedBy = adminUser,
                    TicketPurchase = ticketPurchase
                });
            }
            /* 10 tickets in the same year spread by months */
            for (int i = 0; i < 10; i++)
            {
                var usedDate = new DateTime(now.Year, (i % now.Month) + 1, now.Day).AddHours((i + 8) % now.Hour);
                tickets.Add(new Ticket
                {
                    ExpirationDate = now.AddDays(365),
                    State = TicketState.Used,
                    IsUsed = true,
                    UsedDate = usedDate,
                    Owner = employeeUser!,
                    ValidationCode = Guid.NewGuid().ToString("N")[..8].ToUpper(),
                    ValidatedBy = adminUser,
                    TicketPurchase = ticketPurchase
                });
            }

            if (!context.Ticket.Any(t => t.Owner.Id == employeeUser!.Id))
            {
                await context.Ticket.AddRangeAsync(tickets);
            }
            await context.SaveChangesAsync();

            // Create Orders
            var orders = new List<Order>();
            var products = context.Product.ToList();
            var rnd = new Random();

            // Local auxilliary function to create orders
            Order CreateOrder(DateTime orderDate)
            {
                var order = new Order
                {
                    OrderDate = orderDate,
                    AppUser = employeeUser!,
                    Status = OrderStatus.Delivered,
                    PickupTime = orderDate.TimeOfDay,
                    DeliveryTime = null
                };

                int numberOfLines = rnd.Next(1, products.Count + 1);
                var selectedProducts = products
                    .OrderBy(_ => rnd.Next())
                    .Take(numberOfLines)
                    .ToList();
                foreach (var product in selectedProducts)
                {
                    var quantity = rnd.Next(1, 4);

                    var line = new OrderLine
                    {
                        Product = product,
                        ProductId = product.Id,
                        Order = order,
                        OrderId = order.Id,
                        Quantity = quantity,
                        ProductValue = product.Price
                    };

                    order.ProductPurchases.Add(line);
                }

                order.TotalValue = order.ProductPurchases.Sum(l => l.ProductValue * l.Quantity);
                return order;
            }

            /* 10 orders in the same day with different time */
            for (int i = 0; i < 10; i++)
            {
                var hour = 8 + (i % hourRange); // Start at 8h, end at current hour, loop if more than available hours
                var date = now.Date.AddHours(hour); // 08h–17h but no future time
                orders.Add(CreateOrder(date));
            }
            
            /* 10 orders in the same week, in different days */
            
            for (int i = 0; i < 10; i++)
            {
                var day = (i % dayOfWeekRange) + 1; // Start at Sunday, end at current day, loop if more than available days
                var date = startOfWeek.AddDays(day).AddHours(8 + (i % 8));
                if (date > now) date = date.Date.AddHours(8 + (i % hourRange)); // If future time, set to current day with hour loop
                orders.Add(CreateOrder(date));
            }
            
            /* 10 orders in the same month in different days */
            for (int i = 0; i < 10; i++)
            {
                var day = ((i * 3) % dayOfMonthRange) + 1; // Start at 1st, end at current day, loop if more than available days
                var date = startOfMonth.AddDays(day).AddHours(8 + (i % 8));
                if (date > now) date = date.Date.AddHours(8 + (i % hourRange)); // If future time, set to current day with hour loop
                orders.Add(CreateOrder(date));
            }
            
            /* 10 orders in the same year in different months */
            for (int i = 0; i < 10; i++)
            {
                var date = new DateTime(now.Year, (i % now.Month) + 1, now.Day).AddHours((i + 8) % now.Hour);
                orders.Add(CreateOrder(date));
            }

            if (!context.Order.Any(o => o.AppUser.Id == employeeUser!.Id))
            {
                await context.Order.AddRangeAsync(orders);
            }
            await context.SaveChangesAsync();
        }
    }
}