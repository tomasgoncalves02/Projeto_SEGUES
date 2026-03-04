using Microsoft.EntityFrameworkCore;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models.Inventory;
using Projeto_SEGUES.Models.User;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.Order;
using Projeto_SEGUES.Services;
using Xunit;

namespace SeguesTests.Services
{/*
    public class BarServiceTests
    {
        private AppDbContext GetDatabaseContext() => new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

        [Fact]
        public async Task GetBalanceAsync_UserExists_ReturnsBalance()
        {
            var context = GetDatabaseContext();
            var service = new BarService(context);

            var cat = new UserCategory { Name = "Cliente" };
            context.Users.Add(new AppUser { Id = "u1", FirstName = "A", LastName = "B", UserCategory = cat, BirthDate = new DateTime(2000, 1, 1), Gender = Gender.Other, Balance = 50m });
            await context.SaveChangesAsync();

            var result = await service.GetBalanceAsync("u1");

            Assert.Equal(50m, result);
        }

        [Fact]
        public async Task GetBalanceAsync_UserDoesNotExist_ReturnsZero()
        {
            var context = GetDatabaseContext();
            var service = new BarService(context);

            var result = await service.GetBalanceAsync("u-fantasma");

            Assert.Equal(0m, result);
        }

        [Fact]
        public async Task GetAvailableProductsAsync_ReturnsOnlyActiveInStockBarProducts()
        {
            var context = GetDatabaseContext();
            var service = new BarService(context);

            var barCat = new ProductCategory { Name = "Bar" };
            var otherCat = new ProductCategory { Name = "Cantina" };

            context.Products.AddRange(
                new Product { Id = 1, Name = "P1", Description = "D1", Price = 1m, Stock = 10, IsActive = true, Category = barCat },
                new Product { Id = 2, Name = "P2", Description = "D2", Price = 1m, Stock = 0, IsActive = true, Category = barCat },
                new Product { Id = 3, Name = "P3", Description = "D3", Price = 1m, Stock = 10, IsActive = false, Category = barCat },
                new Product { Id = 4, Name = "P4", Description = "D4", Price = 1m, Stock = 10, IsActive = true, Category = otherCat }
            );
            await context.SaveChangesAsync();

            var result = await service.GetAvailableProductsAsync();

            Assert.Single(result);
            Assert.Equal(1, result[0].Id);
        }

        [Fact]
        public async Task GetOrderHistoryAsync_ReturnsOrderedList()
        {
            var context = GetDatabaseContext();
            var service = new BarService(context);

            var cat = new UserCategory { Name = "Cliente" };
            var user = new AppUser { Id = "u1", FirstName = "A", LastName = "B", UserCategory = cat, BirthDate = new DateTime(2000, 1, 1), Gender = Gender.Other };
            var pCat = new ProductCategory { Name = "Bar" };
            var product = new Product { Id = 1, Name = "P1", Description = "D", Price = 1m, Category = pCat };

            context.Users.Add(user);
            context.Products.Add(product);

            context.BarOrders.AddRange(
                new BarOrder { UserId = "u1", ProductId = 1, PriceAtTime = 1m, OrderDate = DateTime.Now.AddDays(-1), CreationTime = DateOnly.FromDateTime(DateTime.Now), Expired = DateOnly.FromDateTime(DateTime.Now) },
                new BarOrder { UserId = "u1", ProductId = 1, PriceAtTime = 1m, OrderDate = DateTime.Now, CreationTime = DateOnly.FromDateTime(DateTime.Now), Expired = DateOnly.FromDateTime(DateTime.Now) }
            );
            await context.SaveChangesAsync();

            var result = await service.GetOrderHistoryAsync("u1");

            Assert.Equal(2, result.Count);
            Assert.True(result[0].OrderDate > result[1].OrderDate);
        }

        [Fact]
        public async Task PlaceOrderAsync_Success_UpdatesEntities()
        {
            var context = GetDatabaseContext();
            var service = new BarService(context);

            var cat = new UserCategory { Name = "Cliente" };
            var user = new AppUser { Id = "u1", FirstName = "A", LastName = "B", UserCategory = cat, BirthDate = new DateTime(2000, 1, 1), Gender = Gender.Other, Balance = 10m };
            var pCat = new ProductCategory { Name = "Bar" };
            var product = new Product { Id = 1, Name = "P1", Description = "D", Price = 2m, Stock = 5, Category = pCat };

            context.Users.Add(user);
            context.Products.Add(product);
            await context.SaveChangesAsync();

            var result = await service.PlaceOrderAsync("u1", 1);

            Assert.True(result.Succeeded);

            var dbUser = await context.Users.FindAsync("u1");
            var dbProduct = await context.Products.FindAsync(1);
            var dbOrder = await context.BarOrders.FirstOrDefaultAsync();

            Assert.Equal(8m, dbUser!.Balance);
            Assert.Equal(4, dbProduct!.Stock);
            Assert.NotNull(dbOrder);
            Assert.Equal(2m, dbOrder.PriceAtTime);
            Assert.Equal("u1", dbOrder.UserId);
        }

        [Fact]
        public async Task PlaceOrderAsync_InsufficientBalance_ReturnsFalse()
        {
            var context = GetDatabaseContext();
            var service = new BarService(context);

            var cat = new UserCategory { Name = "Cliente" };
            var user = new AppUser { Id = "u1", FirstName = "A", LastName = "B", UserCategory = cat, BirthDate = new DateTime(2000, 1, 1), Gender = Gender.Other, Balance = 1m };
            var pCat = new ProductCategory { Name = "Bar" };
            var product = new Product { Id = 1, Name = "P1", Description = "D", Price = 2m, Stock = 5, Category = pCat };

            context.Users.Add(user);
            context.Products.Add(product);
            await context.SaveChangesAsync();

            var result = await service.PlaceOrderAsync("u1", 1);

            Assert.False(result.Succeeded);
            Assert.Equal("Saldo insuficiente.", result.Message);
        }

        [Fact]
        public async Task PlaceOrderAsync_OutOfStock_ReturnsFalse()
        {
            var context = GetDatabaseContext();
            var service = new BarService(context);

            var cat = new UserCategory { Name = "Cliente" };
            var user = new AppUser { Id = "u1", FirstName = "A", LastName = "B", UserCategory = cat, BirthDate = new DateTime(2000, 1, 1), Gender = Gender.Other, Balance = 10m };
            var pCat = new ProductCategory { Name = "Bar" };
            var product = new Product { Id = 1, Name = "P1", Description = "D", Price = 2m, Stock = 0, Category = pCat };

            context.Users.Add(user);
            context.Products.Add(product);
            await context.SaveChangesAsync();

            var result = await service.PlaceOrderAsync("u1", 1);

            Assert.False(result.Succeeded);
            Assert.Equal("Produto esgotado.", result.Message);
        }
    }*/
}