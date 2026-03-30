using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.Ticket;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;
using Xunit;

namespace SeguesTests.RegressionTests.Services
{
    public class TicketRegressionTests
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

        private AppUser CreatePedro(string id, string catName = "Student") => new()
        {
            Id = id,
            FirstName = "Pedro",
            LastName = "Regression",
            Email = $"{id}@test.pt",
            UserName = $"{id}@test.pt",
            Balance = 100m,
            BirthDate = DateTime.Now.AddYears(-20),
            Gender = Gender.Male,
            UserCategory = new UserCategory { Name = catName }
        };

        [Fact]
        public async Task BuyTicketsAsync_NegativeQuantity_ReturnsFailure()
        {
            var context = GetContext();
            var service = new TicketService(context, Mock.Of<ILogger<TicketService>>());

            var result = await service.BuyTicketsAsync("u1", -10);

            Assert.False(result.Success);
            Assert.Equal("Quantidade inválida.", result.Message);
        }

        [Fact]
        public async Task GetCurrentPriceForUserAsync_NoPriceConfigured_ReturnsZero()
        {
            var context = GetContext();
            var service = new TicketService(context, Mock.Of<ILogger<TicketService>>());
            var pedro = CreatePedro("u1");

            context.Users.Add(pedro);
            await context.SaveChangesAsync();

            var result = await service.GetCurrentPriceForUserAsync(pedro);

            Assert.Equal(0m, result);
        }

        [Fact]
        public async Task TransferTicketsAsync_EmptySelection_ReturnsFailure()
        {
            var context = GetContext();
            var service = new TicketService(context, Mock.Of<ILogger<TicketService>>());

            var result = await service.TransferTicketsAsync("s1", "dest@test.pt", new List<string>());

            Assert.False(result.Success);
            Assert.Equal("Nenhuma senha foi selecionada.", result.Message);
        }

        [Fact]
        public async Task GetActiveTicketsAsync_AutomaticallyUpdatesExpiredTickets()
        {
            var context = GetContext();
            var service = new TicketService(context, Mock.Of<ILogger<TicketService>>());

            var user = CreatePedro("p1");
            var ticket = new Ticket
            {
                ValidationCode = "EXP-REG",
                State = TicketState.Available,
                ExpirationDate = DateTime.Now.AddDays(-5),
                Owner = user,
                TicketPurchase = new TicketPurchase
                {
                    AppUser = user,
                    Value = 1,
                    Quantity = 1,
                    TransactionDate = DateTime.Now.AddDays(-10)
                }
            };

            context.Users.Add(user);
            context.Ticket.Add(ticket);
            await context.SaveChangesAsync();

            var activeTickets = await service.GetActiveTicketsAsync("p1");

            Assert.Empty(activeTickets);

            var dbTicket = await context.Ticket.AsNoTracking().FirstAsync();
            Assert.Equal(TicketState.Expired, dbTicket.State);
        }

        [Fact]
        public async Task BuyTicketsAsync_UserNotFound_ReturnsFailure()
        {
            var context = GetContext();
            var service = new TicketService(context, Mock.Of<ILogger<TicketService>>());

            var result = await service.BuyTicketsAsync("non-existent-id", 1);

            Assert.False(result.Success);
            Assert.Equal("Utilizador não encontrado.", result.Message);
        }
    }
}