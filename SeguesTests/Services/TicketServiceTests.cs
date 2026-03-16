using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models.Admin;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.Ticket;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;
using Xunit;

namespace SeguesTests.Services
{
    public class TicketServiceTests
    {
        private AppDbContext GetDatabaseContext()
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

        private AppUser CreatePedroUser(string id, string email, UserCategory cat, decimal balance = 100m) => new()
        {
            Id = id,
            FirstName = "Pedro",
            LastName = "Ticket",
            Email = email,
            UserName = email,
            UserCategory = cat,
            BirthDate = DateTime.Now.AddYears(-20),
            Gender = Gender.Male,
            Balance = balance
        };

        // Ensures the service correctly identifies the active price for a user based on their category
        [Fact]
        public async Task GetCurrentPriceForUserAsync_ReturnsCorrectPrice()
        {
            var context = GetDatabaseContext();
            var service = new TicketService(context);
            var cat = new UserCategory { Name = "Student" };
            var user = CreatePedroUser("u1", "p@pt.pt", cat);

            var price = new TicketPrice { Price = 2.5m, UserCategory = cat, InitialDatePrice = DateTime.Now.AddDays(-1), EndDatePrice = DateTime.Now.AddDays(1) };

            context.UserCategory.Add(cat);
            context.Users.Add(user);
            context.TicketPrice.Add(price);
            await context.SaveChangesAsync();

            var result = await service.GetCurrentPriceForUserAsync(user);

            Assert.Equal(2.5m, result);
        }

        // Verifies that buying tickets deducts the correct amount and creates the entities
        [Fact]
        public async Task BuyTicketsAsync_Success_UpdatesBalanceAndCreatesTickets()
        {
            var context = GetDatabaseContext();
            var service = new TicketService(context);
            var cat = new UserCategory { Name = "Student" };
            var user = CreatePedroUser("u1", "p@pt.pt", cat, balance: 10m);
            var price = new TicketPrice { Price = 2m, UserCategory = cat, InitialDatePrice = DateTime.Now.AddDays(-1), EndDatePrice = DateTime.Now.AddDays(1) };

            var config = new AppConfig
            {
                Id = 1,
                TicketValidityDays = 30,
                OpenBarTime = TimeSpan.Zero,
                CloseBarTime = TimeSpan.Zero,
                OpenLunchTime = TimeSpan.Zero,
                CloseLunchTime = TimeSpan.Zero,
                OpenDinnerTime = TimeSpan.Zero,
                CloseDinnerTime = TimeSpan.Zero
            };

            context.UserCategory.Add(cat);
            context.Users.Add(user);
            context.TicketPrice.Add(price);
            context.AppConfig.Add(config);
            await context.SaveChangesAsync();

            var result = await service.BuyTicketsAsync("u1", 2);

            Assert.True(result.Success);
            Assert.Equal(6m, user.Balance);
            Assert.Equal(2, await context.Ticket.CountAsync());
        }

        // Ensures that tickets past their expiration date are automatically marked as Expired when queried
        [Fact]
        public async Task GetUserTicketsAsync_AutoExpiresOverdueTickets()
        {
            var context = GetDatabaseContext();
            var service = new TicketService(context);
            var cat = new UserCategory { Name = "Student" };
            var user = CreatePedroUser("u1", "p@pt.pt", cat);

            context.Users.Add(user);
            await context.SaveChangesAsync();

            var purchase = new TicketPurchase { AppUser = user, Quantity = 1, TransactionDate = DateTime.Now.AddDays(-10), Value = 2.5m };
            var expiredTicket = new Ticket
            {
                ValidationCode = "OLD1",
                State = TicketState.Available,
                ExpirationDate = DateTime.Now.AddDays(-1),
                Owner = user,
                TicketPurchase = purchase
            };

            context.Ticket.Add(expiredTicket);
            await context.SaveChangesAsync();

            context.ChangeTracker.Clear();

            var result = await service.GetUserTicketsAsync("u1");

            Assert.Equal(TicketState.Expired, result.First().State);
        }

        // Verifies that a ticket state changes to Used and records the validator staff member
        [Fact]
        public async Task ValidateTicketAsync_ValidCode_SetsToUsed()
        {
            var context = GetDatabaseContext();
            var service = new TicketService(context);
            var cat = new UserCategory { Name = "Employee" };
            var owner = CreatePedroUser("u1", "o@pt.pt", cat);
            var validator = CreatePedroUser("v1", "staff@pt.pt", cat);

            var purchase = new TicketPurchase { AppUser = owner, Quantity = 1, TransactionDate = DateTime.Now, Value = 2.5m };
            var ticket = new Ticket { ValidationCode = "VALID123", State = TicketState.Available, ExpirationDate = DateTime.Now.AddDays(1), Owner = owner, TicketPurchase = purchase };

            context.Users.AddRange(owner, validator);
            context.Ticket.Add(ticket);
            await context.SaveChangesAsync();

            var result = await service.ValidateTicketAsync("VALID123", validator);

            Assert.True(result.Success);
            Assert.Equal(TicketState.Used, ticket.State);
            Assert.Equal("v1", ticket.ValidatedBy?.Id);
        }

        // Verifies that tickets are successfully transferred between users of the same category
        [Fact]
        public async Task TransferTicketsAsync_Success_ChangesOwnership()
        {
            var context = GetDatabaseContext();
            var service = new TicketService(context);
            var cat = new UserCategory { Name = "Student" };
            var sender = CreatePedroUser("s1", "sender@pt.pt", cat);
            var receiver = CreatePedroUser("r1", "receiver@pt.pt", cat);

            context.Users.AddRange(sender, receiver);
            await context.SaveChangesAsync();

            var purchase = new TicketPurchase { AppUser = sender, Quantity = 1, Value = 2m, TransactionDate = DateTime.Now };
            var ticket = new Ticket { ValidationCode = "T1", State = TicketState.Available, Owner = sender, ExpirationDate = DateTime.Now.AddDays(1), TicketPurchase = purchase };

            context.Ticket.Add(ticket);
            await context.SaveChangesAsync();

            var result = await service.TransferTicketsAsync("s1", "receiver@pt.pt", new List<string> { "T1" });

            Assert.True(result.Success);
            var updatedTicket = await context.Ticket.Include(t => t.Owner).FirstAsync();
            Assert.Equal("r1", updatedTicket.Owner.Id);
        }

        // Blocks transfers when the recipient belongs to a different user category
        [Fact]
        public async Task TransferTicketsAsync_DifferentCategories_FailsValidation()
        {
            var context = GetDatabaseContext();
            var service = new TicketService(context);
            var cat1 = new UserCategory { Name = "Student" };
            var cat2 = new UserCategory { Name = "Employee" };

            var sender = CreatePedroUser("s1", "s@pt.pt", cat1);
            var receiver = CreatePedroUser("r1", "r@pt.pt", cat2);

            context.UserCategory.AddRange(cat1, cat2);
            context.Users.AddRange(sender, receiver);
            await context.SaveChangesAsync();

            var result = await service.TransferTicketsAsync("s1", "r@pt.pt", new List<string> { "T1" });

            Assert.False(result.Success);
            Assert.Contains("Transferência recusada", result.Message);
        }
    }
}