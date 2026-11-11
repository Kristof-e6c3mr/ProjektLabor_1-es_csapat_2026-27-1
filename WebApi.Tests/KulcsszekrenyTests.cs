using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using WebApi;
using WebApi.Data;

using Xunit;

namespace WebApi.Tests
{
    public class KulcsSzekrenyTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private static readonly string SeedPassword = Environment.GetEnvironmentVariable("SEED_PASSWORD") ?? "Titkos123!";

        private const string AdminEmail = "admin@uni.hu";
        private const string PortasEmail = "portas@uni.hu";
        private const string OktatoEmail = "kiss.o@uni.hu";

        public KulcsSzekrenyTests(WebApplicationFactory<Program> factory)
        {
            string dbName = "TestDb_Szekreny_" + Guid.NewGuid();

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
        public async Task SzekrenyLekerdezese_TokenNelkul_401esValasztAd()
        {
            var client = _factory.CreateClient();

            var response = await client.GetAsync("/api/kulcsmozgas/virtualis-szekreny");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task SzekrenyLekerdezese_OktatoSzerepkorrel_403asValasztAd()
        {
            var client = _factory.CreateClient();
            var token = await BejelentkezesAsync(client, OktatoEmail, SeedPassword);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync("/api/kulcsmozgas/virtualis-szekreny");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task SzekrenyLekerdezese_PortasSzerepkorrel_SikeresVisszateresEsLista()
        {
            var client = _factory.CreateClient();
            var token = await BejelentkezesAsync(client, PortasEmail, SeedPassword);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync("/api/kulcsmozgas/virtualis-szekreny");

            Assert.True(response.IsSuccessStatusCode);
            var szekreny = await response.Content.ReadFromJsonAsync<List<VirtualisKulcsDto>>();

            Assert.NotNull(szekreny);
            Assert.NotEmpty(szekreny);
        }

        [Fact]
        public async Task SzekrenyLekerdezese_AdminSzerepkorrel_SikeresVisszateresEsLista()
        {
            var client = _factory.CreateClient();
            var token = await BejelentkezesAsync(client, AdminEmail, SeedPassword);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync("/api/kulcsmozgas/virtualis-szekreny");

            Assert.True(response.IsSuccessStatusCode);
            var szekreny = await response.Content.ReadFromJsonAsync<List<VirtualisKulcsDto>>();

            Assert.NotNull(szekreny);
            Assert.NotEmpty(szekreny);
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

        private sealed class VirtualisKulcsDto
        {
            public int KulcsID { get; set; }
            public string Megnevezes { get; set; } = "";
            public string Statusz { get; set; } = "";
            public string AtvevoNev { get; set; } = "";
            public string Teremszam { get; set; } = "";
            public bool MesterkulcsE { get; set; }
        }
    }
}