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
    public class AdminHibaJegyTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private static readonly string SeedPassword = Environment.GetEnvironmentVariable("SEED_PASSWORD") ?? "Titkos123!";
        private const string AdminEmail = "admin@uni.hu";
        private const string OktatoEmail = "kiss.o@uni.hu";

        public AdminHibaJegyTests(WebApplicationFactory<Program> factory)
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
        public async Task HibakLekerdezese_TokenNelkul_401esValasztAd()
        {
            var client = _factory.CreateClient();

            var response = await client.GetAsync("/api/hibak");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task HibakLekerdezese_AdminSzerepkorrel_SikeresVisszateresEsLista()
        {
            var client = _factory.CreateClient();
            var token = await BejelentkezesAsync(client, AdminEmail, SeedPassword);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync("/api/hibak");

            Assert.True(response.IsSuccessStatusCode);
            var hibak = await response.Content.ReadFromJsonAsync<List<HibaAdminDto>>();

            Assert.NotNull(hibak);
            Assert.NotEmpty(hibak);
        }

        [Fact]
        public async Task StatuszModositas_AdminSzerepkorrel_SikeresenFrissit()
        {
            var client = _factory.CreateClient();
            var token = await BejelentkezesAsync(client, AdminEmail, SeedPassword);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var hibak = await client.GetFromJsonAsync<List<HibaAdminDto>>("/api/hibak");
            Assert.NotNull(hibak);
            var tesztHiba = hibak.First();
            var ujAllapot = !tesztHiba.Allapot;

            var dto = new StatuszModositasDto { Allapot = ujAllapot };
            var response = await client.PutAsJsonAsync($"/api/hibak/{tesztHiba.HibaID}/statusz", dto);

            Assert.True(response.IsSuccessStatusCode);

            var frissitettHibak = await client.GetFromJsonAsync<List<HibaAdminDto>>("/api/hibak");
            var ellenorzesHiba = frissitettHibak!.First(h => h.HibaID == tesztHiba.HibaID);

            Assert.Equal(ujAllapot, ellenorzesHiba.Allapot);
        }

        [Fact]
        public async Task HibaTorlese_AdminSzerepkorrel_SikeresenTorol()
        {
            var client = _factory.CreateClient();
            var token = await BejelentkezesAsync(client, AdminEmail, SeedPassword);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var hibak = await client.GetFromJsonAsync<List<HibaAdminDto>>("/api/hibak");
            Assert.NotNull(hibak);
            var torlendoHiba = hibak.Last();

            var deleteResponse = await client.DeleteAsync($"/api/hibak/{torlendoHiba.HibaID}");

            Assert.True(deleteResponse.IsSuccessStatusCode);

            var maradekHibak = await client.GetFromJsonAsync<List<HibaAdminDto>>("/api/hibak");
            Assert.DoesNotContain(maradekHibak!, h => h.HibaID == torlendoHiba.HibaID);
        }

        [Fact]
        public async Task HibakLekerdezese_JogosulatlanSzerepkorrel_403asValasztAd()
        {
            var client = _factory.CreateClient();
            var token = await BejelentkezesAsync(client, OktatoEmail, SeedPassword);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync("/api/hibak");
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
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

        private sealed class HibaAdminDto
        {
            public int HibaID { get; set; }
            public string Teremszam { get; set; } = "";
            public string PortasNev { get; set; } = "";
            public string Leiras { get; set; } = "";
            public DateTime LetrehozasDatuma { get; set; }
            public bool Allapot { get; set; }
        }

        private sealed class StatuszModositasDto
        {
            public bool Allapot { get; set; }
        }
    }
}