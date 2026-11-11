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
    public class KarbantartasTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private static readonly string SeedPassword = Environment.GetEnvironmentVariable("SEED_PASSWORD") ?? "Titkos123!";
        private const string AdminEmail = "admin@uni.hu";
        private const string OktatoEmail = "kiss.o@uni.hu";

        public KarbantartasTests(WebApplicationFactory<Program> factory)
        {
            string dbName = "TestDb_Karbantartas_" + Guid.NewGuid();

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
        public async Task KarbantartasokLekerdezese_TokenNelkul_401esValasztAd()
        {
            var client = _factory.CreateClient();

            var response = await client.GetAsync("/api/karbantartasiidosav");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task KarbantartasokLekerdezese_OktatoSzerepkorrel_403asValasztAd()
        {
            var client = _factory.CreateClient();
            var token = await BejelentkezesAsync(client, OktatoEmail, SeedPassword);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync("/api/karbantartasiidosav");

            // Teacher doesn't have access to this Endpoint
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task UjKarbantartas_SzabadIdosavban_SikeresenRogzitodik()
        {
            var client = _factory.CreateClient();
            var token = await BejelentkezesAsync(client, AdminEmail, SeedPassword);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var termek = await client.GetFromJsonAsync<List<TeremDto>>("/api/termek");
            Assert.NotNull(termek);
            var i2Terem = termek.First(t => t.Teremszam == "I2");

            var kezdet = DateTime.UtcNow.Date.AddDays(5).AddHours(8);
            var ujKarbantartas = new KarbantartasMentesDto
            {
                KarbantartasID = 0,
                TeremID = i2Terem.TeremID,
                KezdoIdo = kezdet,
                ZaroIdo = kezdet.AddDays(1),
                Leiras = "Tervezett festés az I2 teremben"
            };

            var response = await client.PostAsJsonAsync("/api/karbantartasiidosav", ujKarbantartas);

            Assert.True(response.IsSuccessStatusCode);

            
            var karbantartasok = await client.GetFromJsonAsync<List<KarbantartasDto>>("/api/karbantartasiidosav");
            Assert.Contains(karbantartasok!, k => k.Leiras == ujKarbantartas.Leiras);
        }

        [Fact]
        public async Task UtkozesEllenorzes_FoglaltIdosavban_VisszaadjaAzUtkozest()
        {
            var client = _factory.CreateClient();
            var token = await BejelentkezesAsync(client, AdminEmail, SeedPassword);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var termek = await client.GetFromJsonAsync<List<TeremDto>>("/api/termek");
            var i1Terem = termek!.First(t => t.Teremszam == "I1");

            var holnapDel = DateTime.UtcNow.Date.AddDays(1).AddHours(12);
            var utkozesKeres = new
            {
                KarbantartasID = 0,
                TeremID = i1Terem.TeremID,
                KezdoIdo = holnapDel.AddHours(-2),
                ZaroIdo = holnapDel.AddHours(2)
            };

            var response = await client.PostAsJsonAsync("/api/karbantartasiidosav/utkozesek", utkozesKeres);
            Assert.True(response.IsSuccessStatusCode);

            var eredmeny = await response.Content.ReadFromJsonAsync<UtkozesEredmenyDto>();

            Assert.NotNull(eredmeny);
            Assert.NotEmpty(eredmeny.Foglalasok);
        }

        [Fact]
        public async Task KarbantartasTorlese_AdminSzerepkorrel_SikeresenTorol()
        {
            var client = _factory.CreateClient();
            var token = await BejelentkezesAsync(client, AdminEmail, SeedPassword);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var termek = await client.GetFromJsonAsync<List<TeremDto>>("/api/termek");
            var i3Terem = termek!.First(t => t.Teremszam == "I3");

            var kezdet = DateTime.UtcNow.Date.AddDays(10).AddHours(8);
            var dto = new KarbantartasMentesDto
            {
                KarbantartasID = 0,
                TeremID = i3Terem.TeremID,
                KezdoIdo = kezdet,
                ZaroIdo = kezdet.AddDays(1),
                Leiras = "Törlendő karbantartás"
            };
            await client.PostAsJsonAsync("/api/karbantartasiidosav", dto);

            var karbantartasok = await client.GetFromJsonAsync<List<KarbantartasDto>>("/api/karbantartasiidosav");
            var torlendoEszkoz = karbantartasok!.First(k => k.Leiras == "Törlendő karbantartás");

            var deleteResponse = await client.DeleteAsync($"/api/karbantartasiidosav/{torlendoEszkoz.KarbantartasID}");
            Assert.True(deleteResponse.IsSuccessStatusCode);

            var maradek = await client.GetFromJsonAsync<List<KarbantartasDto>>("/api/karbantartasiidosav");
            Assert.DoesNotContain(maradek!, k => k.KarbantartasID == torlendoEszkoz.KarbantartasID);
        }

        private static async Task<string> BejelentkezesAsync(HttpClient client, string username, string password)
        {
            var loginResponse = await client.PostAsJsonAsync("/api/login", new { Username = username, Password = password });
            loginResponse.EnsureSuccessStatusCode();
            var result = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
            Assert.False(string.IsNullOrWhiteSpace(result?.Token));
            return result!.Token;
        }

        private sealed class LoginResponse { public string Token { get; set; } = ""; }

        private sealed class TeremDto { public int TeremID { get; set; } public string Teremszam { get; set; } = ""; }

        private sealed class KarbantartasMentesDto
        {
            public int KarbantartasID { get; set; }
            public int TeremID { get; set; }
            public DateTime KezdoIdo { get; set; }
            public DateTime ZaroIdo { get; set; }
            public string Leiras { get; set; } = "";
        }

        private sealed class KarbantartasDto
        {
            public int KarbantartasID { get; set; }
            public int TeremID { get; set; }
            public string Teremszam { get; set; } = "";
            public DateTime KezdoIdo { get; set; }
            public DateTime ZaroIdo { get; set; }
            public string Leiras { get; set; } = "";
        }

        private sealed class UtkozesEredmenyDto
        {
            public List<UtkozoFoglalasDto> Foglalasok { get; set; } = new();
            public List<UtkozoKarbantartasDto> Karbantartasok { get; set; } = new();
        }

        private sealed class UtkozoFoglalasDto
        {
            public int FoglalasID { get; set; }
            public string Teremszam { get; set; } = "";
            public string OktatoNev { get; set; } = "";
            public DateTime KezdoIdo { get; set; }
            public DateTime ZaroIdo { get; set; }
        }

        private sealed class UtkozoKarbantartasDto
        {
            public int KarbantartasID { get; set; }
            public string Teremszam { get; set; } = "";
            public DateTime KezdoIdo { get; set; }
            public DateTime ZaroIdo { get; set; }
            public string Leiras { get; set; } = "";
        }
    }
}