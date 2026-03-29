using Microsoft.EntityFrameworkCore;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.Order;
using Projeto_SEGUES.Models.Ticket;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;

namespace SeguesTests.UnitTests.Services
{
    public class ReportUnitTests
    {
        private AppDbContext GetContext() =>
            new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

        [Fact]
        public async Task GetOrdersStats_CalculatesAveragesCorrectly()
        {
            var context = GetContext();
            var service = new ReportService(context);
            var pedro = new AppUser { Id = "u1", FirstName = "Pedro", LastName = "S", Balance = 0, BirthDate = DateTime.Now, Gender = Gender.Male, UserCategory = new UserCategory { Name = "X" }, Email = "p@t.pt" };

            context.Order.AddRange(
                new Order { AppUser = pedro, TotalValue = 10m, OrderDate = DateTime.Now, Status = OrderStatus.Delivered, RedemptionCode = "R1" },
                new Order { AppUser = pedro, TotalValue = 20m, OrderDate = DateTime.Now, Status = OrderStatus.Delivered, RedemptionCode = "R2" }
            );
            await context.SaveChangesAsync();

            var result = await service.GetOrdersStats(1);

            Assert.Equal(30m, result.TotalRevenue);
            Assert.Equal(15m, result.AverageRevenue);
            Assert.Equal(2, result.TotalOrders);
        }

        [Fact]
        public async Task GetTicketsStats_CalculatesRevenuePerTicket()
        {
            var context = GetContext();
            var service = new ReportService(context);
            var pedro = new AppUser { Id = "u1", FirstName = "Pedro", LastName = "S", Balance = 0, BirthDate = DateTime.Now, Gender = Gender.Male, UserCategory = new UserCategory { Name = "X" }, Email = "p@t.pt" };
            var purchase = new TicketPurchase { AppUser = pedro, Quantity = 2, Value = 5.0m, TransactionDate = DateTime.Now };

            context.Ticket.Add(new Ticket { ValidationCode = "T1", IsUsed = true, UsedDate = DateTime.Now, Owner = pedro, TicketPurchase = purchase, State = TicketState.Used, ExpirationDate = DateTime.Now.AddDays(1) });
            await context.SaveChangesAsync();

            var result = await service.GetTicketsStats(1);

            Assert.Equal(2.5m, result.TotalRevenue);
        }
    }
}