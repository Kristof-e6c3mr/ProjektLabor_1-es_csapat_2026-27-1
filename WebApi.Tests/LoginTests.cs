using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using System.Net;
using System.Net.Http.Json;
using WebApi;
using WebApi.Data;
using WebApi.Models;
using Xunit;

namespace WebApi.Tests
{
    public class LoginTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public LoginTests(WebApplicationFactory<Program> factory)
        {
            // Felülírjuk a PostgreSQL kapcsolatot egy In-Memory adatbázisra,
            // így a GitHub Actions CI pipeline-ban is le fog futni a teszt.
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                    if (descriptor != null) services.Remove(descriptor);

                    services.AddDbContext<AppDbContext>(options =>
                        options.UseInMemoryDatabase("TestDb_Login_" + Guid.NewGuid().ToString()));
                });
            });
        }

        [Fact]
        public async Task Login_ErvenytelenAdatokkal_UnauthorizedStatusztAd()
        {
            // Arrange: Létrehozunk egy teszt klienst és egy hibás bejelentkezési modellt
            var client = _factory.CreateClient();
            var rosszAdatok = new
            {
                Username = "hibas.felhasznalo@teszt.hu",
                Password = "RosszJelszo123"
            };

            // Act: Elküldjük a POST kérést pontosan a ti végpontotokra
            var response = await client.PostAsJsonAsync("/api/login", rosszAdatok);

            // Assert: Ellenőrizzük, hogy a szerver helyesen, 401 Unauthorized hibával dobja-e vissza
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}