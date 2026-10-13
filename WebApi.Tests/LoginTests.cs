using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebApi;
using WebApi.Data;
using Xunit;

namespace WebApi.Tests
{
    public class LoginTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public LoginTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Törlés: minden DbContext-hez köthető beállítást kisöpörünk
                    var descriptors = services.Where(d =>
                        d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                        d.ServiceType.Name.Contains("DbContextOptions")).ToList();

                    foreach (var descriptor in descriptors)
                    {
                        services.Remove(descriptor);
                    }

                    // Indul a memória adatbázis
                    services.AddDbContext<AppDbContext>(options =>
                        options.UseInMemoryDatabase("TestDb_Login_" + Guid.NewGuid().ToString()));
                });
            });
        }

        [Fact]
        public async Task Login_ErvenytelenAdatokkal_UnauthorizedStatusztAd()
        {
            var client = _factory.CreateClient();
            var rosszAdatok = new { Username = "hibas.felhasznalo@teszt.hu", Password = "RosszJelszo123" };

            var response = await client.PostAsJsonAsync("/api/login", rosszAdatok);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}