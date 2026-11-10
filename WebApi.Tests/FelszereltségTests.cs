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
    public class FelszereltsegTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private static readonly string SeedPassword = Environment.GetEnvironmentVariable("SEED_PASSWORD") ?? "Titkos123!";
        private const string AdminEmail = "admin@uni.hu";
        private const string OktatoEmail = "kiss.o@uni.hu";

        public FelszereltsegTests(WebApplicationFactory<Program> factory)
        {
            string dbName = "TestDb_Felszereltseg_" + Guid.NewGuid();

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

                    // Inicializáljuk a memóriabeli adatbázist,
                    // ami létrehozza az alapértelmezett felszereltségeket (pl. Projektor, Okostábla).
                    DbInitializer.Initialize(db);
                });
            });
        }

        [Fact]
        public async Task FelszereltsegekLekerdezese_TokenNelkul_401esValasztAd()
        {
            var client = _factory.CreateClient();

            var response = await client.GetAsync("/api/felszereltseg");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task FelszereltsegekLekerdezese_AdminSzerepkorrel_SikeresVisszateresEsLista()
        {
            var client = _factory.CreateClient();
            var token = await BejelentkezesAsync(client, AdminEmail, SeedPassword);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync("/api/felszereltseg");

            Assert.True(response.IsSuccessStatusCode);
            var felszereltsegek = await response.Content.ReadFromJsonAsync<List<FelszereltsegDto>>();

            Assert.NotNull(felszereltsegek);
            Assert.NotEmpty(felszereltsegek); // cannot be empty because of DBinit
        }

        [Fact]
        public async Task UjFelszereltsegHozzaadasa_AdminSzerepkorrel_SikeresenRogzitodik()
        {
            var client = _factory.CreateClient();
            var token = await BejelentkezesAsync(client, AdminEmail, SeedPassword);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var ujFelszereltsegNev = "3D Nyomtató Teszt";
            var dto = new FelszereltsegInputDto { Nev = ujFelszereltsegNev };

            var response = await client.PostAsJsonAsync("/api/felszereltseg", dto);

            Assert.True(response.IsSuccessStatusCode);

            var felszereltsegek = await client.GetFromJsonAsync<List<FelszereltsegDto>>("/api/felszereltseg");
            Assert.NotNull(felszereltsegek);
            Assert.Contains(felszereltsegek, f => f.Nev == ujFelszereltsegNev);
        }

        [Fact]
        public async Task FelszereltsegTorlese_AdminSzerepkorrel_SikeresenTorol()
        {
            var client = _factory.CreateClient();
            var token = await BejelentkezesAsync(client, AdminEmail, SeedPassword);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var torlendoNev = "Törlendő Felszerelés";
            await client.PostAsJsonAsync("/api/felszereltseg", new FelszereltsegInputDto { Nev = torlendoNev });

            var felszereltsegek = await client.GetFromJsonAsync<List<FelszereltsegDto>>("/api/felszereltseg");
            Assert.NotNull(felszereltsegek);
            var torlendoEszkoz = felszereltsegek.First(f => f.Nev == torlendoNev);

            var deleteResponse = await client.DeleteAsync($"/api/felszereltseg/{torlendoEszkoz.FelszereltsegID}");
            Assert.True(deleteResponse.IsSuccessStatusCode);

            var maradekEszkozok = await client.GetFromJsonAsync<List<FelszereltsegDto>>("/api/felszereltseg");
            Assert.DoesNotContain(maradekEszkozok!, f => f.FelszereltsegID == torlendoEszkoz.FelszereltsegID);
        }

        [Fact]
        public async Task UjFelszereltsegHozzaadasa_OktatoSzerepkorrel_403asValasztAd()
        {
            var client = _factory.CreateClient();
            var token = await BejelentkezesAsync(client, OktatoEmail, SeedPassword);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var dto = new FelszereltsegInputDto { Nev = "Jogosulatlan Felszerelés" };

            var response = await client.PostAsJsonAsync("/api/felszereltseg", dto);

            // not admin cannot create new accessory
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task FelszereltsegTorlese_OktatoSzerepkorrel_403asValasztAd()
        {
            var client = _factory.CreateClient();
            var token = await BejelentkezesAsync(client, OktatoEmail, SeedPassword);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);


            var response = await client.DeleteAsync("/api/felszereltseg/1");

            // not admin cannot delete accessory
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

        private sealed class FelszereltsegDto
        {
            public int FelszereltsegID { get; set; }
            public string Nev { get; set; } = "";
        }

        private sealed class FelszereltsegInputDto
        {
            public string Nev { get; set; } = "";
        }
    }
}