using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.Ticket;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;

namespace SeguesTests.UnitTests.Services
{
    public class TicketUnitTests
    {
        private AppDbContext GetContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
            return new AppDbContext(options);
        }

        [Fact]
        public async Task GetCurrentPriceForUserAsync_ReturnsMostRecentActivePrice()
        {
            var context = GetContext();
            var service = new TicketService(context, Mock.Of<ILogger<TicketService>>());

            var cat = new UserCategory { Id = 1, Name = "Pedro-Student" };

            var pedro = new AppUser
            {
                Id = "u1",
                FirstName = "Pedro",
                LastName = "S",
                UserCategory = cat,
                BirthDate = DateTime.Now.AddYears(-20),
                Gender = Gender.Male,
                Balance = 0,
                Email = "p@t.pt",
                UserName = "p@t.pt"
            };

            context.Users.Add(pedro);

            context.TicketPrice.AddRange(
                new TicketPrice { Price = 2.0m, UserCategory = cat, InitialDatePrice = DateTime.Now.AddDays(-10), EndDatePrice = DateTime.Now.AddDays(-5) },
                new TicketPrice { Price = 2.5m, UserCategory = cat, InitialDatePrice = DateTime.Now.AddDays(-1), EndDatePrice = DateTime.Now.AddDays(1) }
            );

            await context.SaveChangesAsync();

            var result = await service.GetCurrentPriceForUserAsync(pedro);

            Assert.Equal(2.5m, result);
        }
    }
}