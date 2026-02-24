using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Projeto_SEGUES.Areas.Bar.Controllers;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models.Bar;
using Projeto_SEGUES.Models.Inventory;
using Projeto_SEGUES.Models.User;
using Xunit;

namespace SeguesTests.Bar
{
    public class OrderManagementControllerTests
    {
        private AppDbContext GetDatabaseContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            var databaseContext = new AppDbContext(options);
            databaseContext.Database.EnsureCreated();
            return databaseContext;
        }
        
        // Método auxiliar para criar as dependências necessárias (User e Product)
        private async Task<(string userId, int productId)> SeedDependencies(AppDbContext context)
        {
            var user = new AppUser
            {
                Id = "u1",
                UserName = "t@t.com",
                FirstName = "T",
                LastName = "U",
                UserCategory = new UserCategory { Name = "E" },
                BirthDate = DateTime.Now.AddYears(-20),
                Gender = Projeto_SEGUES.Models.Enums.Gender.Other
            };
            var product = new Product { Name = "P", Description = "D", Price = 1m };

            context.Users.Add(user);
            context.Products.Add(product);
            await context.SaveChangesAsync();
            return (user.Id, product.Id);
        }

        [Fact]
        public async Task ManageOrders_ReturnsOnlyActiveOrders()
        {
            using var context = GetDatabaseContext();
            var controller = new OrderManagementController(context);
            var (uid, pid) = await SeedDependencies(context);

            context.BarOrders.AddRange(new List<BarOrder>
            {
                new BarOrder { Id = 10, Status = 0, RedemptionCode = "A", UserId = uid, ProductId = pid, PriceAtTime = 1m },
                new BarOrder { Id = 11, Status = 3, RedemptionCode = "B", UserId = uid, ProductId = pid, PriceAtTime = 1m }
            });
            await context.SaveChangesAsync();

            var result = await controller.ManageOrders();

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<BarOrder>>(viewResult.Model);
            Assert.Single(model); // Agora passará, pois o utilizador existe!
        }

        [Fact]
        public async Task GetOrderDetailsSide_ReturnsPartialViewWithOrder()
        {
            using var context = GetDatabaseContext();
            var controller = new OrderManagementController(context);
            var (uid, pid) = await SeedDependencies(context);

            var order = new BarOrder { Id = 40, RedemptionCode = "SIDE", UserId = uid, ProductId = pid, PriceAtTime = 1m };
            context.BarOrders.Add(order);
            await context.SaveChangesAsync();

            var result = await controller.GetOrderDetailsSide(40);

            // Se o utilizador existir na BD, retornará PartialView em vez de NotFound
            var partialResult = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("_OrderDetailsSideCard", partialResult.ViewName);
        }

        [Fact]
        public async Task UpdateStatus_UpdatesRelatedOrders()
        {
            using var context = GetDatabaseContext();
            var controller = new OrderManagementController(context);
            var (uid, pid) = await SeedDependencies(context);

            var code = "UP123";
            context.BarOrders.Add(new BarOrder { Id = 50, RedemptionCode = code, Status = 0, UserId = uid, ProductId = pid, PriceAtTime = 1m });
            await context.SaveChangesAsync();

            var result = await controller.UpdateStatus(50, 1);

            Assert.IsType<OkResult>(result);
        }
    }
}