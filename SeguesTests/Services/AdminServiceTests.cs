using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Projeto_SEGUES.Areas.Admin.ViewModels;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.Ticket;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Services;
using Xunit;

namespace SeguesTests.Services
{
    public class AdminServiceTests
    {
        private AppDbContext GetDatabaseContext() => new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

        private Mock<UserManager<AppUser>> GetMockUserManager(AppDbContext context)
        {
            var mock = new Mock<UserManager<AppUser>>(new Mock<IUserStore<AppUser>>().Object, null, null, null, null, null, null, null, null);
            mock.Setup(m => m.Users).Returns(context.Users);
            return mock;
        }

        private Mock<RoleManager<Role>> GetMockRoleManager(AppDbContext context)
        {
            var mock = new Mock<RoleManager<Role>>(new Mock<IRoleStore<Role>>().Object, null, null, null, null);
            mock.Setup(m => m.Roles).Returns(context.Roles);
            return mock;
        }

        [Fact]
        public async Task CreateInternalUserAsync_InvalidRole_ReturnsFailed()
        {
            var context = GetDatabaseContext();
            var mockUserMgr = GetMockUserManager(context);
            var mockRoleMgr = GetMockRoleManager(context);
            var mockEmailSender = new Mock<IEmailSender>();

            var service = new AdminService(context, mockUserMgr.Object, mockRoleMgr.Object, mockEmailSender.Object);

            mockRoleMgr.Setup(m => m.FindByNameAsync("Inexistente")).ReturnsAsync((Role)null!);

            var model = new CreateInternalUserViewModel
            {
                AccountType = "Inexistente",
                Email = "teste@teste.pt",
                FirstName = "Nome",
                LastName = "Apelido",
                Gender = Gender.Other,
                BirthDate = new DateTime(1990, 1, 1)
            };

            var result = await service.CreateInternalUserAsync(model);

            Assert.False(result.Succeeded);
            Assert.Contains(result.Errors, e => e.Description.Contains("Dados inválidos"));
        }

        [Fact]
        public async Task GetFilteredUsersAsync_CategoryFilter_ReturnsMatch()
        {
            var context = GetDatabaseContext();
            var service = new AdminService(context, GetMockUserManager(context).Object, GetMockRoleManager(context).Object, new Mock<IEmailSender>().Object);

            var catDocente = new UserCategory { Name = "Docente" };
            var catEstudante = new UserCategory { Name = "Estudante" };
            context.UserCategories.AddRange(catDocente, catEstudante);

            context.Users.Add(new AppUser { Id = "u1", FirstName = "A", LastName = "B", Email = "a@a.pt", UserCategory = catDocente, BirthDate = new DateTime(2000, 1, 1), Gender = Gender.Other });
            context.Users.Add(new AppUser { Id = "u2", FirstName = "C", LastName = "D", Email = "c@c.pt", UserCategory = catEstudante, BirthDate = new DateTime(2000, 1, 1), Gender = Gender.Other });
            await context.SaveChangesAsync();

            var result = await service.GetFilteredUsersAsync(null, null, "Docente");

            Assert.Single(result);
            Assert.Equal("u1", result[0].Id);
        }

        [Fact]
        public async Task GetFilteredUsersAsync_SearchByName_ReturnsMatch()
        {
            var context = GetDatabaseContext();
            var service = new AdminService(context, GetMockUserManager(context).Object, GetMockRoleManager(context).Object, new Mock<IEmailSender>().Object);

            var cat = new UserCategory { Name = "Cliente" };
            context.UserCategories.Add(cat);

            context.Users.Add(new AppUser { Id = "u1", FirstName = "Diogo", LastName = "Silva", Email = "diogo@pt.pt", UserCategory = cat, BirthDate = new DateTime(1995, 1, 1), Gender = Gender.Male });
            context.Users.Add(new AppUser { Id = "u2", FirstName = "Joao", LastName = "Costa", Email = "joao@pt.pt", UserCategory = cat, BirthDate = new DateTime(1995, 1, 1), Gender = Gender.Male });
            await context.SaveChangesAsync();

            var result = await service.GetFilteredUsersAsync("diogo", null, null);

            Assert.Single(result);
            Assert.Equal("Diogo", result[0].FirstName);
        }

        [Fact]
        public async Task GetCategoryByNameAsync_ReturnsCategory()
        {
            var context = GetDatabaseContext();
            var service = new AdminService(context, GetMockUserManager(context).Object, GetMockRoleManager(context).Object, new Mock<IEmailSender>().Object);

            var cat = new UserCategory { Name = "Externo" };
            context.UserCategories.Add(cat);
            await context.SaveChangesAsync();

            var result = await service.GetCategoryByNameAsync("Externo");

            Assert.NotNull(result);
            Assert.Equal("Externo", result.Name);
        }

        [Fact]
        public async Task UpdateTicketPricesAsync_UpdatesPrice()
        {
            var context = GetDatabaseContext();
            var service = new AdminService(context, GetMockUserManager(context).Object, GetMockRoleManager(context).Object, new Mock<IEmailSender>().Object);

            var cat = new UserCategory { Name = "Estudante" };
            var price = new TicketPrice { Id = 1, Price = 2.0m, UserCategory = cat, InitialDatePrice = DateTime.Now.AddDays(-5), EndDatePrice = DateTime.Now.AddDays(10) };
            context.UserCategories.Add(cat);
            context.TicketPrices.Add(price);
            await context.SaveChangesAsync();

            var newPrices = new List<TicketPrice>
    {
        new TicketPrice { Id = 1, Price = 3.5m, UserCategory = cat }
    };

            await service.UpdateTicketPricesAsync(newPrices);

            var updated = await context.TicketPrices.FindAsync(1);
            Assert.NotNull(updated);
            Assert.Equal(3.5m, updated.Price);
            Assert.Equal(DateTime.Today.AddDays(1).AddTicks(-1).Date, updated.EndDatePrice.Date);
        }
    }
}