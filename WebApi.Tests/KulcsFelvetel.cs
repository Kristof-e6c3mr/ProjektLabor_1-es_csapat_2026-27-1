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
    public class KulcsmozgasTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private static readonly string SeedPassword = Environment.GetEnvironmentVariable("SEED_PASSWORD") ?? "Titkos123!";

        private const string PortasEmail = "portas@uni.hu";
        private const string OktatoEmail = "kiss.o@uni.hu";

        public KulcsmozgasTests(WebApplicationFactory<Program> factory)
        {
            string dbName = "TestDb_Kulcsmozgas_" + Guid.NewGuid();

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
        public async Task Azonositas_TokenNelkul_401esValasztAd()
        {
            var client = _factory.CreateClient();
            var keresDto = new PinKodKeresDto { PinKod = "123456" };

            var response = await client.PostAsJsonAsync("/api/kulcsmozgas/azonositas", keresDto);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Azonositas_OktatoSzerepkorrel_403asValasztAd()
        {
            var client = _factory.CreateClient();
            var token = await BejelentkezesAsync(client, OktatoEmail, SeedPassword);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var keresDto = new PinKodKeresDto { PinKod = "123456" };
            var response = await client.PostAsJsonAsync("/api/kulcsmozgas/azonositas", keresDto);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Azonositas_PortasSzerepkorrelEsHelyesPinKoddal_SikeresValasztAd()
        {
            var client = _factory.CreateClient();
            var token = await BejelentkezesAsync(client, PortasEmail, SeedPassword);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var oktato = await db.Felhasznalok.FirstAsync(f => f.Email == OktatoEmail);

            var keresDto = new PinKodKeresDto { PinKod = oktato.PinKod! };
            var response = await client.PostAsJsonAsync("/api/kulcsmozgas/azonositas", keresDto);

            Assert.True(response.IsSuccessStatusCode);
            var result = await response.Content.ReadFromJsonAsync<OktatoDto>();
            Assert.NotNull(result);
            Assert.Equal(oktato.Id, result.Id);
            Assert.Equal(oktato.Nev, result.Nev);
        }

        [Fact]
        public async Task KulcsKiadas_PortasSzerepkorrel_SikeresenRogzitodik()
        {
            var client = _factory.CreateClient();
            var token = await BejelentkezesAsync(client, PortasEmail, SeedPassword);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var oktato = await db.Felhasznalok.FirstAsync(f => f.Email == OktatoEmail);
            var portas = await db.Felhasznalok.FirstAsync(f => f.Email == PortasEmail);
            var i2Terem = await db.Termek.FirstAsync(t => t.Teremszam == "I2");
            var i2Kulcs = await db.Kulcsok.FirstAsync(k => k.TeremID == i2Terem.TeremID);

            var kiadasDto = new KulcsKiadasKeresDto
            {
                KulcsID = i2Kulcs.KulcsID,
                OktatoID = oktato.Id,
                PortasID = portas.Id
            };

            var response = await client.PostAsJsonAsync("/api/kulcsmozgas/kiadas", kiadasDto);

            Assert.True(response.IsSuccessStatusCode);

            var tartozasResponse = await client.GetAsync($"/api/kulcsmozgas/tartozas/{oktato.Id}");
            tartozasResponse.EnsureSuccessStatusCode();
            var tartozasok = await tartozasResponse.Content.ReadFromJsonAsync<List<TartozasDto>>();

            Assert.NotNull(tartozasok);
            Assert.Contains(tartozasok, t => t.KulcsID == i2Kulcs.KulcsID);
        }

        [Fact]
        public async Task KulcsVisszavetele_PortasSzerepkorrel_SikeresenLezarjaAMozgast()
        {
            var client = _factory.CreateClient();
            var token = await BejelentkezesAsync(client, PortasEmail, SeedPassword);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var oktato = await db.Felhasznalok.FirstAsync(f => f.Email == OktatoEmail);
            var portas = await db.Felhasznalok.FirstAsync(f => f.Email == PortasEmail);

            var i3Terem = await db.Termek.FirstAsync(t => t.Teremszam == "I3");
            var i3Kulcs = await db.Kulcsok.FirstAsync(k => k.TeremID == i3Terem.TeremID);

            await client.PostAsJsonAsync("/api/kulcsmozgas/kiadas", new KulcsKiadasKeresDto
            {
                KulcsID = i3Kulcs.KulcsID,
                OktatoID = oktato.Id,
                PortasID = portas.Id
            });

            var visszavetelResponse = await client.PostAsync($"/api/kulcsmozgas/visszavetel/{i3Kulcs.KulcsID}", null);
            Assert.True(visszavetelResponse.IsSuccessStatusCode);

            var tartozasResponse = await client.GetAsync($"/api/kulcsmozgas/tartozas/{oktato.Id}");
            var tartozasok = await tartozasResponse.Content.ReadFromJsonAsync<List<TartozasDto>>();

            Assert.DoesNotContain(tartozasok!, t => t.KulcsID == i3Kulcs.KulcsID);
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

        private sealed class PinKodKeresDto
        {
            public string PinKod { get; set; } = "";
        }

        private sealed class OktatoDto
        {
            public int Id { get; set; }
            public string Nev { get; set; } = "";
        }

        private sealed class KulcsKiadasKeresDto
        {
            public int KulcsID { get; set; }
            public int OktatoID { get; set; }
            public int PortasID { get; set; }
        }

        private sealed class TartozasDto
        {
            public int KulcsID { get; set; }
            public string Megnevezes { get; set; } = "";
            public DateTime Kiadva { get; set; }
        }
    }
}
