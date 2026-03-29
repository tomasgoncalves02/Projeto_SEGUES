using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Projeto_SEGUES.Data;
using Projeto_SEGUES.Models.User;
using SeguesTests.Helpers;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Xunit;

namespace Tests.IntegrationTests.Users;

public class UserControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public UserControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null) services.Remove(descriptor);

                services.AddDbContext<AppDbContext>(options =>
                    options.UseInMemoryDatabase("IntegrationTestDb"));

                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "Test";
                    options.DefaultChallengeScheme = "Test";
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", null);

                services.AddSingleton<IAntiforgery, NoOpAntiforgery>();
            });
        });

        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test", "User");
    }

    [Fact]
    public async Task UpdateProfile_Integration_SuccessfullyUpdatesDatabase()
    {
        var userId = "pedro-77";

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            var cat = new UserCategory { Name = "Cliente" };
            db.UserCategory.Add(cat);
            await db.SaveChangesAsync();

            var user = new AppUser
            {
                Id = userId,
                UserName = "Pedro",
                Email = "pedro@segues.pt",
                FirstName = "Pedro",
                LastName = "Original",
                BirthDate = new System.DateTime(2000, 1, 1),
                Gender = Projeto_SEGUES.Models.Enums.Gender.Male,
                UserCategory = cat
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();
        }

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "Id",        userId },
            { "FirstName", "Pedro Alterado" },
            { "LastName",  "Jesus" },
            { "Email",     "pedro@segues.pt" },
            { "Gender",    "Male" },
            { "BirthDate", "2000-01-01" },
            { "Category",  "Cliente" }
        });

        var response = await _client.PostAsync("/User/User/UpdateProfile", content);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var updatedUser = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);

            Assert.NotNull(updatedUser);
            Assert.Equal("Pedro Alterado", updatedUser.FirstName);
        }
    }
}