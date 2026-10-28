using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using WebApi;
using WebApi.Data;

namespace WebApi.Tests
{
    public class HibaTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private static readonly string SeedPassword = Environment.GetEnvironmentVariable("SEED_PASSWORD") ?? "Titkos123!";

        public HibaTests(WebApplicationFactory<Program> factory)
        {
            string dbName = "TestDb_Hiba_" + Guid.NewGuid();

            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptors = services.Where(d =>
                        d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                        d.ServiceType.Name.Contains("DbContextOptions")).ToList();

                    foreach (var descriptor in descriptors)
                    {
                        services.Remove(descriptor);
                    }

                    services.AddDbContext<AppDbContext>(options =>
                        options.UseInMemoryDatabase(dbName));

                    var sp = services.BuildServiceProvider();
                    using var scope = sp.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    DbInitializer.Initialize(db);
                });
            });
        }

        [Fact]
        public async Task HibajegyMentes_OktatoSzerepkorrel_403asValasztAd()
        {
            var client = _factory.CreateClient();
            var token = await BejelentkezesAsync(client, "kiss.o@uni.hu", SeedPassword); // Teacher
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var ujHiba = new
            {
                Leiras = "Nem működik a projektor.",
                TeremID = 1,
                PortasID = 3
            };

            var response = await client.PostAsJsonAsync("/api/hibak", ujHiba);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task HibajegyMentes_PortasSzerepkorrel_SikeresenRogzitodik()
        {
            var client = _factory.CreateClient();
            var token = await BejelentkezesAsync(client, "portas@uni.hu", SeedPassword); // Reception
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var ujHiba = new
            {
                Leiras = "Kiégett az izzó az I1-es teremben.",
                TeremID = 1,
                PortasID = 3 // Nagy Portás ID
            };

            var response = await client.PostAsJsonAsync("/api/hibak", ujHiba);

            Assert.True(response.IsSuccessStatusCode);
        }

        private static async Task<string> BejelentkezesAsync(HttpClient client, string username, string password)
        {
            var loginResponse = await client.PostAsJsonAsync("/api/login", new { Username = username, Password = password });
            loginResponse.EnsureSuccessStatusCode();
            var result = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
            Assert.False(string.IsNullOrWhiteSpace(result?.Token));
            return result!.Token;
        }

        private sealed class LoginResponse
        {
            public string Token { get; set; } = "";
        }
    }
}