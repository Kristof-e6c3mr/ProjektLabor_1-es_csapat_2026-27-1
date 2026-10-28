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
    public class FoglalasTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private static readonly string SeedPassword = Environment.GetEnvironmentVariable("SEED_PASSWORD") ?? "Titkos123!";
        private const string OktatoEmail = "kiss.o@uni.hu";

        public FoglalasTests(WebApplicationFactory<Program> factory)
        {
            string dbName = "TestDb_Foglalas_" + Guid.NewGuid();

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
        public async Task SajtFoglalasok_TokenNelkul_401esValasztAd()
        {
            var client = _factory.CreateClient();

            var response = await client.GetAsync("/api/foglalasok/sajat/4");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task UjFoglalas_PortasSzerepkorrel_403asValasztAd()
        {
            var client = _factory.CreateClient();
            var token = await BejelentkezesAsync(client, "portas@uni.hu", SeedPassword);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var kezdet = DateTime.UtcNow.Date.AddDays(2).AddHours(8);
            var ujFoglalas = new UjFoglalasDto
            {
                TeremID = 1,
                OktatoID = 4, // kiss.o@uni.hu
                KezdoIdo = kezdet,
                ZaroIdo = kezdet.AddMinutes(90)
            };

            var response = await client.PostAsJsonAsync("/api/foglalasok", ujFoglalas);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task UjFoglalas_OktatokentSzabadIdosavban_SikeresenRogzitodik()
        {
            var client = _factory.CreateClient();
            var token = await BejelentkezesAsync(client, OktatoEmail, SeedPassword);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var termek = await client.GetFromJsonAsync<List<TeremDto>>("/api/termek");
            Assert.NotNull(termek);
            var i2Terem = termek.First(t => t.Teremszam == "I2");

            var kezdet = DateTime.UtcNow.Date.AddDays(3).AddHours(10);
            var ujFoglalas = new UjFoglalasDto
            {
                TeremID = i2Terem.TeremID,
                OktatoID = 4,
                KezdoIdo = kezdet,
                ZaroIdo = kezdet.AddMinutes(90)
            };

            var response = await client.PostAsJsonAsync("/api/foglalasok", ujFoglalas);

            Assert.True(response.IsSuccessStatusCode);
        }

        [Fact]
        public async Task UjFoglalas_UtkozoIdosavban_HibatAdVissza()
        {
            var client = _factory.CreateClient();
            var token = await BejelentkezesAsync(client, OktatoEmail, SeedPassword);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var termek = await client.GetFromJsonAsync<List<TeremDto>>("/api/termek");
            var i1Terem = termek!.First(t => t.Teremszam == "I1");

            //  I1 already reserved tomorrow between 12:00 - 13:30
            var holnapDel = DateTime.UtcNow.Date.AddDays(1).AddHours(12);
            var utkozoFoglalas = new UjFoglalasDto
            {
                TeremID = i1Terem.TeremID,
                OktatoID = 4,
                KezdoIdo = holnapDel.AddMinutes(30), // 12:30 - 14:00 colliding
                ZaroIdo = holnapDel.AddMinutes(120)
            };

            var response = await client.PostAsJsonAsync("/api/foglalasok", utkozoFoglalas);

            Assert.False(response.IsSuccessStatusCode);
        }

        [Fact]
        public async Task FoglalasTorlese_SajatFoglalasEseten_SikeresenTorol()
        {
            var client = _factory.CreateClient();
            var token = await BejelentkezesAsync(client, OktatoEmail, SeedPassword);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var termek = await client.GetFromJsonAsync<List<TeremDto>>("/api/termek");
            var i3Terem = termek!.First(t => t.Teremszam == "I3");

            var kezdet = DateTime.UtcNow.Date.AddDays(4).AddHours(8);
            await client.PostAsJsonAsync("/api/foglalasok", new UjFoglalasDto
            {
                TeremID = i3Terem.TeremID,
                OktatoID = 4,
                KezdoIdo = kezdet,
                ZaroIdo = kezdet.AddMinutes(90)
            });

            var sajatFoglalasok = await client.GetFromJsonAsync<List<FoglalasListaDto>>("/api/foglalasok/sajat/4");
            Assert.NotNull(sajatFoglalasok);
            var torlendo = sajatFoglalasok.Last();

            var deleteResponse = await client.DeleteAsync($"/api/foglalasok/{torlendo.FoglalasID}");

            Assert.True(deleteResponse.IsSuccessStatusCode);
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

        private sealed class TeremDto
        {
            public int TeremID { get; set; }
            public string Teremszam { get; set; } = "";
        }

        private sealed class UjFoglalasDto
        {
            public int TeremID { get; set; }
            public int OktatoID { get; set; }
            public DateTime KezdoIdo { get; set; }
            public DateTime ZaroIdo { get; set; }
        }

        private sealed class FoglalasListaDto
        {
            public int FoglalasID { get; set; }
            public string Teremszam { get; set; } = "";
            public DateTime KezdoIdo { get; set; }
            public DateTime ZaroIdo { get; set; }
            public string Allapot { get; set; } = "";
        }
    }
}