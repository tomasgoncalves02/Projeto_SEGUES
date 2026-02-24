using Microsoft.AspNetCore.Identity;
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
        private AppDbContext GetDatabaseContext() => new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options);

        private Mock<UserManager<AppUser>> GetMockUserManager() =>
            new Mock<UserManager<AppUser>>(new Mock<IUserStore<AppUser>>().Object, null, null, null, null, null, null, null, null);

        private Mock<RoleManager<Role>> GetMockRoleManager() =>
            new Mock<RoleManager<Role>>(new Mock<IRoleStore<Role>>().Object, null, null, null, null);

        [Fact]
        public async Task GetCurrentPriceForUserAsync_ReturnsCorrectPrice()
        {
            var context = GetDatabaseContext();
            var service = new TicketService(context, GetMockUserManager().Object, GetMockRoleManager().Object);

            var cat = new UserCategory { Name = "Estudante" };
            var user = new AppUser { Id = "u1", FirstName = "A", LastName = "B", UserCategory = cat, BirthDate = new DateTime(2000, 1, 1), Gender = Gender.Other };
            var price = new TicketPrice { Price = 2.5m, UserCategory = cat, InitialDatePrice = DateTime.Now.AddDays(-1), EndDatePrice = DateTime.Now.AddDays(1) };

            context.UserCategories.Add(cat);
            context.Users.Add(user);
            context.TicketPrices.Add(price);
            await context.SaveChangesAsync();

            var result = await service.GetCurrentPriceForUserAsync(user);

            Assert.Equal(2.5m, result);
        }

        [Fact]
        public async Task BuyTicketsAsync_Success_UpdatesBalanceAndCreatesTickets()
        {
            var context = GetDatabaseContext();
            var service = new TicketService(context, GetMockUserManager().Object, GetMockRoleManager().Object);

            var cat = new UserCategory { Id = 1, Name = "Estudante" };
            var user = new AppUser { Id = "u1", FirstName = "A", LastName = "B", UserCategory = cat, BirthDate = new DateTime(2000, 1, 1), Gender = Gender.Other, Balance = 10m };
            var price = new TicketPrice { Price = 2m, UserCategory = cat, InitialDatePrice = DateTime.Now.AddDays(-1), EndDatePrice = DateTime.Now.AddDays(1) };
            var config = new AppConfig { Id = 1, TicketValidityDays = 30 };

            context.UserCategories.Add(cat);
            context.Users.Add(user);
            context.TicketPrices.Add(price);
            context.AppConfigs.Add(config);
            await context.SaveChangesAsync();

            var result = await service.BuyTicketsAsync("u1", 2);

            Assert.True(result.Success);
            Assert.Equal(6m, user.Balance);
            Assert.Equal(2, await context.Tickets.CountAsync());
        }

        [Fact]
        public async Task ValidateTicketAsync_ValidCode_SetsToUsed()
        {
            var context = GetDatabaseContext();
            var service = new TicketService(context, GetMockUserManager().Object, GetMockRoleManager().Object);

            var cat = new UserCategory { Name = "Estudante" };
            var owner = new AppUser { Id = "u1", FirstName = "A", LastName = "B", UserCategory = cat, BirthDate = new DateTime(2000, 1, 1), Gender = Gender.Other };
            var validator = new AppUser { Id = "v1", FirstName = "V", LastName = "V", UserCategory = cat, BirthDate = new DateTime(1990, 1, 1), Gender = Gender.Other };

            var purchase = new TicketPurchase { AppUser = owner, Quantity = 1, TransactionDate = DateTime.Now, Value = 2.5m };
            var ticket = new Ticket { ValidationCode = "VALID123", State = TicketState.Available, ExpirationDate = DateTime.Now.AddDays(1), Owner = owner, TicketPurchase = purchase };

            context.Users.AddRange(owner, validator);
            context.TicketPurchases.Add(purchase);
            context.Tickets.Add(ticket);
            await context.SaveChangesAsync();

            var result = await service.ValidateTicketAsync("VALID123", validator);

            Assert.True(result.Success);
            Assert.Equal(TicketState.Used, ticket.State);
            Assert.True(ticket.IsUsed);
            Assert.Equal("v1", ticket.ValidatedBy?.Id);
        }

        [Fact]
        public async Task TransferTicketsAsync_Success_ChangesOwner()
        {
            var context = GetDatabaseContext();
            var service = new TicketService(context, GetMockUserManager().Object, GetMockRoleManager().Object);

            var cat = new UserCategory { Name = "Estudante" };
            var sender = new AppUser { Id = "s1", Email = "sender@pt.pt", FirstName = "S", LastName = "S", UserCategory = cat, BirthDate = new DateTime(2000, 1, 1), Gender = Gender.Other };
            var receiver = new AppUser { Id = "r1", Email = "receiver@pt.pt", FirstName = "R", LastName = "R", UserCategory = cat, BirthDate = new DateTime(2000, 1, 1), Gender = Gender.Other };

            var purchase = new TicketPurchase { AppUser = sender, Quantity = 1, TransactionDate = DateTime.Now, Value = 2.5m };
            var ticket = new Ticket { ValidationCode = "TICKET1", State = TicketState.Available, Owner = sender, ExpirationDate = DateTime.Now.AddDays(1), TicketPurchase = purchase };

            context.Users.AddRange(sender, receiver);
            context.UserCategories.Add(cat);
            context.TicketPurchases.Add(purchase);
            context.Tickets.Add(ticket);
            await context.SaveChangesAsync();

            var result = await service.TransferTicketsAsync("s1", "receiver@pt.pt", new List<string> { "TICKET1" });

            Assert.True(result.Success);
            Assert.Equal("r1", ticket.Owner.Id);
            Assert.Equal(1, await context.TicketTransfers.CountAsync());
        }

        [Fact]
        public async Task TransferTicketsAsync_DifferentCategories_Fails()
        {
            var context = GetDatabaseContext();
            var service = new TicketService(context, GetMockUserManager().Object, GetMockRoleManager().Object);

            var cat1 = new UserCategory { Id = 1, Name = "Estudante" };
            var cat2 = new UserCategory { Id = 2, Name = "Professor" };

            var sender = new AppUser { Id = "s1", Email = "s@pt.pt", FirstName = "S", LastName = "S", UserCategory = cat1, BirthDate = new DateTime(2000, 1, 1), Gender = Gender.Other };
            var receiver = new AppUser { Id = "r1", Email = "r@pt.pt", FirstName = "R", LastName = "R", UserCategory = cat2, BirthDate = new DateTime(2000, 1, 1), Gender = Gender.Other };

            context.UserCategories.AddRange(cat1, cat2);
            context.Users.AddRange(sender, receiver);
            await context.SaveChangesAsync();

            var result = await service.TransferTicketsAsync("s1", "r@pt.pt", new List<string> { "T1" });

            Assert.False(result.Success);
            Assert.Contains("Transferência recusada", result.Message);
        }
    }
}