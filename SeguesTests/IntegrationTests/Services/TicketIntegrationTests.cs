using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models.Admin;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.Ticket;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;
using Xunit;

namespace SeguesTests.IntegrationTests.Services
{
    public class TicketIntegrationTests
    {
        private AppDbContext GetContext()
        {
            var connection = new SqliteConnection("Filename=:memory:");
            connection.Open();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connection)
                .Options;

            var context = new AppDbContext(options);
            context.Database.EnsureCreated();

            return context;
        }

        private AppUser CreatePedro(string id, string email, UserCategory cat) => new()
        {
            Id = id,
            FirstName = "Pedro",
            LastName = "Integration",
            Email = email,
            UserName = email,
            UserCategory = cat,
            BirthDate = DateTime.Now.AddYears(-20),
            Gender = Gender.Male,
            Balance = 100m
        };

        [Fact]
        public async Task BuyTicketsAsync_FullTransaction_DeductsBalanceAndCreatesAllRecords()
        {
            var context = GetContext();
            var service = new TicketService(context, Mock.Of<ILogger<TicketService>>());

            var cat = new UserCategory { Name = "Student" };
            var pedro = CreatePedro("u1", "pedro@test.pt", cat);
            pedro.Balance = 10m;

            context.UserCategory.Add(cat);
            context.Users.Add(pedro);
            context.TicketPrice.Add(new TicketPrice { Price = 2.5m, UserCategory = cat, InitialDatePrice = DateTime.Now.AddDays(-1) });
            context.AppConfig.Add(new AppConfig { Id = 1, TicketValidityDays = 30 });
            await context.SaveChangesAsync();

            var result = await service.BuyTicketsAsync("u1", 2);

            var updatedUser = await context.Users.FindAsync("u1");
            var ticketsCount = await context.Ticket.CountAsync(t => t.Owner.Id == "u1");
            var hasTransaction = await context.Transaction.AnyAsync(t => t.User.Id == "u1" && t.Amount == -5m);
            var hasPurchase = await context.TicketPurchase.AnyAsync(p => p.AppUser.Id == "u1" && p.Quantity == 2);

            Assert.True(result.Success);
            Assert.Equal(5m, updatedUser!.Balance);
            Assert.Equal(2, ticketsCount);
            Assert.True(hasTransaction);
            Assert.True(hasPurchase);
        }

        [Fact]
        public async Task TransferTicketsAsync_AtomicOperation_ChangesOwnerAndLogsTransfer()
        {
            var context = GetContext();
            var service = new TicketService(context, Mock.Of<ILogger<TicketService>>());

            var cat = new UserCategory { Name = "Student" };
            var pedroSender = CreatePedro("s1", "sender@test.pt", cat);
            var pedroReceiver = CreatePedro("r1", "receiver@test.pt", cat);

            context.UserCategory.Add(cat);
            context.Users.AddRange(pedroSender, pedroReceiver);

            var purchase = new TicketPurchase { AppUser = pedroSender, Quantity = 1, Value = 2m, TransactionDate = DateTime.Now };
            var ticket = new Ticket
            {
                ValidationCode = "TRANSFER-1",
                Owner = pedroSender,
                State = TicketState.Available,
                ExpirationDate = DateTime.Now.AddDays(1),
                TicketPurchase = purchase
            };

            context.Ticket.Add(ticket);
            await context.SaveChangesAsync();

            await service.TransferTicketsAsync("s1", "receiver@test.pt", new List<string> { "TRANSFER-1" });

            var dbTicket = await context.Ticket.Include(t => t.Owner).FirstAsync();
            var transferRecord = await context.TicketTransfer.FirstOrDefaultAsync(tr => tr.Ticket.ValidationCode == "TRANSFER-1");

            Assert.Equal("r1", dbTicket.Owner.Id);
            Assert.NotNull(transferRecord);
            Assert.Equal("s1", transferRecord.Sender.Id);
            Assert.Equal("r1", transferRecord.Receiver.Id);
        }

        [Fact]
        public async Task ValidateTicketAsync_UpdatesDatabaseStateAndStaffMember()
        {
            var context = GetContext();
            var service = new TicketService(context, Mock.Of<ILogger<TicketService>>());

            var cat = new UserCategory { Name = "Employee" };
            var pedroOwner = CreatePedro("o1", "owner@test.pt", cat);
            var pedroStaff = CreatePedro("staff1", "staff@test.pt", cat);

            var purchase = new TicketPurchase { AppUser = pedroOwner, Quantity = 1, Value = 2m, TransactionDate = DateTime.Now };
            var ticket = new Ticket
            {
                ValidationCode = "VAL-123",
                Owner = pedroOwner,
                State = TicketState.Available,
                ExpirationDate = DateTime.Now.AddDays(1),
                TicketPurchase = purchase
            };

            context.UserCategory.Add(cat);
            context.Users.AddRange(pedroOwner, pedroStaff);
            context.Ticket.Add(ticket);
            await context.SaveChangesAsync();

            await service.ValidateTicketAsync("VAL-123", pedroStaff);

            var updatedTicket = await context.Ticket.Include(t => t.ValidatedBy).FirstAsync();

            Assert.Equal(TicketState.Used, updatedTicket.State);
            Assert.True(updatedTicket.IsUsed);
            Assert.Equal("staff1", updatedTicket.ValidatedBy?.Id);
            Assert.NotNull(updatedTicket.UsedDate);
        }
    }
}