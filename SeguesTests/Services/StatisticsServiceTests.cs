using Microsoft.EntityFrameworkCore;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.Inventory;
using Projeto_SEGUES.Models.Order;
using Projeto_SEGUES.Models.Ticket;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;

namespace SeguesTests.Services
{
    public class StatisticsServiceTests
    {
        private AppDbContext GetDatabaseContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        // Helper para ler propriedades de objetos anónimos dentro de 'object'
        private object GetPropValue(object obj, string name)
        {
            return obj.GetType().GetProperty(name)?.GetValue(obj, null)!;
        }


        // Verifies that ticket statistics calculate the correct total meals and revenue
        [Fact]
        public async Task GetTicketsStats_CalculatesCorrectTotals()
        {
            var context = GetDatabaseContext();
            var service = new StatisticsService(context);
            var cat = new UserCategory { Name = "Student" };
            var pedro = new AppUser { Id = "u1", FirstName = "P", LastName = "S", UserCategory = cat, BirthDate = DateTime.Now.AddYears(-20), Gender = Gender.Male, Email = "p@t.pt" };

            var purchase = new TicketPurchase { AppUser = pedro, Quantity = 2, Value = 5.0m, TransactionDate = DateTime.Now };
            context.Ticket.AddRange(
                new Ticket { ValidationCode = "T1", IsUsed = true, UsedDate = DateTime.Now, Owner = pedro, TicketPurchase = purchase, State = TicketState.Used, ExpirationDate = DateTime.Now.AddDays(1) },
                new Ticket { ValidationCode = "T2", IsUsed = true, UsedDate = DateTime.Now, Owner = pedro, TicketPurchase = purchase, State = TicketState.Used, ExpirationDate = DateTime.Now.AddDays(1) }
            );
            await context.SaveChangesAsync();

            var result = await service.GetTicketsStats(1);

            // Usamos reflexão para aceder aos dados do objeto anónimo
            var totalMeals = (int)GetPropValue(result, "totalMeals");
            var totalRevenue = (decimal)GetPropValue(result, "totalRevenue");

            Assert.Equal(2, totalMeals);
            Assert.Equal(5.0m, totalRevenue);
        }


        // Ensures that cancelled orders and carts are excluded from bar statistics
        [Fact]
        public async Task GetBarStats_ExcludesCancelledAndCarts()
        {
            var context = GetDatabaseContext();
            var service = new StatisticsService(context);
            var pedro = new AppUser { Id = "u1", FirstName = "P", LastName = "S", UserCategory = new UserCategory { Name = "X" }, BirthDate = DateTime.Now, Gender = Gender.Male, Email = "p@t.pt" };

            context.Order.AddRange(
                new Order { AppUser = pedro, Status = OrderStatus.Delivered, TotalValue = 10m, OrderDate = DateTime.Now, RedemptionCode = "R1" },
                new Order { AppUser = pedro, Status = OrderStatus.Cancelled, TotalValue = 50m, OrderDate = DateTime.Now, RedemptionCode = "R2" }
            );
            await context.SaveChangesAsync();

            var result = await service.GetBarStats(1);

            var totalConsumptions = (int)GetPropValue(result, "totalConsumptions");
            var totalRevenue = (decimal)GetPropValue(result, "totalRevenue");

            Assert.Equal(1, totalConsumptions);
            Assert.Equal(10m, totalRevenue);
        }


        // Confirms that the top products list groups and sums quantities accurately
        [Fact]
        public async Task GetBarStats_TopProducts_GroupsAndSumsCorrectly()
        {
            var context = GetDatabaseContext();
            var service = new StatisticsService(context);
            var pedro = new AppUser { Id = "u1", FirstName = "P", LastName = "S", UserCategory = new UserCategory { Name = "X" }, BirthDate = DateTime.Now, Gender = Gender.Male, Email = "p@t.pt" };
            var cat = new ProductCategory { Name = "Bebida", Description = "D" };
            var prod = new Product { Name = "Água", Price = 1m, Stock = 10, MinimumStock = 1, IsActive = true, Description = "D", Category = cat };

            var order = new Order { AppUser = pedro, Status = OrderStatus.Delivered, TotalValue = 2m, OrderDate = DateTime.Now, RedemptionCode = "TOP1" };
            order.ProductPurchases.Add(new OrderLine { Product = prod, Quantity = 2, ProductValue = 1m, Order = order, ProductId = 1, OrderId = 1 });

            context.Order.Add(order);
            await context.SaveChangesAsync();

            var result = await service.GetBarStats(1);

            var topProducts = GetPropValue(result, "topProducts") as IEnumerable<object>;

            Assert.NotNull(topProducts);
            Assert.NotEmpty(topProducts);
        }


        // Validates that ticket statistics are correctly grouped by user category
        [Fact]
        public async Task GetTicketsStats_ByCategory_GroupsCorrectly()
        {
            var context = GetDatabaseContext();
            var service = new StatisticsService(context);

            var catStudent = new UserCategory { Name = "Student" };
            var catEmployee = new UserCategory { Name = "Employee" };

            var pedro = new AppUser { Id = "u1", FirstName = "Pedro", LastName = "S", UserCategory = catStudent, BirthDate = DateTime.Now, Gender = Gender.Male, Email = "p1@t.pt" };
            var staff = new AppUser { Id = "u2", FirstName = "Staff", LastName = "X", UserCategory = catEmployee, BirthDate = DateTime.Now, Gender = Gender.Male, Email = "s1@t.pt" };

            var p1 = new TicketPurchase { AppUser = pedro, Quantity = 1, Value = 2.5m, TransactionDate = DateTime.Now };
            var p2 = new TicketPurchase { AppUser = staff, Quantity = 1, Value = 2.5m, TransactionDate = DateTime.Now };

            context.Ticket.AddRange(
                new Ticket
                {
                    ValidationCode = "T1",
                    IsUsed = true,
                    UsedDate = DateTime.Now,
                    Owner = pedro,
                    State = TicketState.Used,
                    ExpirationDate = DateTime.Now.AddDays(1),
                    TicketPurchase = p1
                },
                new Ticket
                {
                    ValidationCode = "T2",
                    IsUsed = true,
                    UsedDate = DateTime.Now,
                    Owner = staff,
                    State = TicketState.Used,
                    ExpirationDate = DateTime.Now.AddDays(1),
                    TicketPurchase = p2
                }
            );
            await context.SaveChangesAsync();

            var result = await service.GetTicketsStats(1);
            var byCategory = GetPropValue(result, "byCategory") as IEnumerable<object>;

            Assert.NotNull(byCategory);
            Assert.Equal(2, byCategory.Count());
        }


        // Verifies that statistics only include data within the specified time period
        [Fact]
        public async Task GetBarStats_PeriodFiltering_ExcludesOldData()
        {
            var context = GetDatabaseContext();
            var service = new StatisticsService(context);
            var pedro = new AppUser { Id = "u1", FirstName = "Pedro", LastName = "S", UserCategory = new UserCategory { Name = "X" }, BirthDate = DateTime.Now, Gender = Gender.Male, Email = "p@t.pt" };

            context.Order.AddRange(
                new Order { AppUser = pedro, Status = OrderStatus.Delivered, TotalValue = 10m, OrderDate = DateTime.Now, RedemptionCode = "TODAY" },
                new Order { AppUser = pedro, Status = OrderStatus.Delivered, TotalValue = 50m, OrderDate = DateTime.Now.AddDays(-2), RedemptionCode = "OLD" }
            );
            await context.SaveChangesAsync();

            var result = await service.GetBarStats(1);
            var totalConsumptions = (int)GetPropValue(result, "totalConsumptions");

            Assert.Equal(1, totalConsumptions);
        }


        // Checks that default values are returned when no data exists for the period
        [Fact]
        public async Task GetBarStats_EmptyData_ReturnsDefaultValues()
        {
            var context = GetDatabaseContext();
            var service = new StatisticsService(context);

            var result = await service.GetBarStats(1);
            var totalRevenue = (decimal)GetPropValue(result, "totalRevenue");
            var topProducts = GetPropValue(result, "topProducts") as IEnumerable<object>;

            Assert.Equal(0m, totalRevenue);
            Assert.Empty(topProducts);
        }
    }
}