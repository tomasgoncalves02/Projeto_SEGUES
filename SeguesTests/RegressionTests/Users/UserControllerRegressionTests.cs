using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models.Enums;
using Projeto_SEGUES.Models.User;
using SeguesTests.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace SeguesTests.RegressionTests.Users
{
    public class UserControllerRegressionTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public UserControllerRegressionTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Index_UserNotFoundInDatabase_ReturnsChallengeRedirect()
        {
            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                    if (descriptor != null) services.Remove(descriptor);

                    services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase("UserRegDb1"));

                    services.AddAuthentication("Test")
                        .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", null);
                });
            }).CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Database.EnsureDeleted();
                db.Database.EnsureCreated();
            }

            var response = await client.GetAsync("/User/User/Index");

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        }

        [Fact]
        public async Task UpdateProfile_InvalidModelState_ReturnsViewWithErrors()
        {
            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                    if (descriptor != null) services.Remove(descriptor);

                    services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase("UserRegDb2"));

                    services.AddAuthentication("Test")
                        .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", null);

                    services.Configure<MvcOptions>(options => {
                        options.Filters.Add(new IgnoreAntiforgeryTokenAttribute());
                    });
                });
            }).CreateClient();

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Database.EnsureDeleted();
                db.Database.EnsureCreated();

                var cat = new UserCategory { Name = "Cliente" };
                db.UserCategory.Add(cat);

                var role = new Role { Name = "Client", NormalizedName = "CLIENT", DisplayName ="Cliente" };
                db.Roles.Add(role);

                var user = new Student
                {
                    Id = "pedro-77",
                    UserName = "Pedro",
                    Email = "pedro@segues.pt",
                    FirstName = "Pedro",
                    LastName = "Original",
                    BirthDate = new DateTime(2000, 1, 1),
                    Gender = Gender.Male,
                    UserCategory = cat,
                    StudentNumber = "2024001"
                };

                db.Users.Add(user);
                await db.SaveChangesAsync();
            }

            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "Id", "pedro-77" },
                { "FirstName", "" }
            });

            var response = await client.PostAsync("/User/User/UpdateProfile", content);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}