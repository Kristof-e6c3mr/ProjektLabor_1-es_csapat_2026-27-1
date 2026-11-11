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
    public class MesterkulcsTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private static readonly string SeedPassword = Environment.GetEnvironmentVariable("SEED_PASSWORD") ?? "Titkos123!";

        private const string AdminEmail = "admin@uni.hu";
        private const string OktatoEmail = "kiss.o@uni.hu";

        public MesterkulcsTests(WebApplicationFactory<Program> factory)
        {
            string dbName = "TestDb_MesterkulcsJog_" + Guid.NewGuid();

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
        public async Task Lekerdezes_TokenNelkul_401esValasztAd()
        {
            var client = _factory.CreateClient();

            var response = await client.GetAsync("/api/mesterkulcsjogosultsag");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Lekerdezes_OktatoSzerepkorrel_403asValasztAd()
        {
            var client = _factory.CreateClient();
            var token = await BejelentkezesAsync(client, OktatoEmail, SeedPassword);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync("/api/mesterkulcsjogosultsag");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task UjJogosultsag_AdminSzerepkorrel_SikeresenMentve()
        {
            var client = _factory.CreateClient();
            var token = await BejelentkezesAsync(client, AdminEmail, SeedPassword);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var felhasznalok = await client.GetFromJsonAsync<List<FelhasznaloDto>>("/api/mesterkulcsjogosultsag/felhasznalok");
            var mesterkulcsok = await client.GetFromJsonAsync<List<MesterkulcsDto>>("/api/mesterkulcsjogosultsag/mesterkulcsok");

            Assert.NotNull(felhasznalok);
            Assert.NotNull(mesterkulcsok);

            var oktato = felhasznalok.First(f => f.Nev.Contains("Oktató"));
            var mesterkulcs = mesterkulcsok.First();

            var ujJogosultsag = new JogosultsagMentesDto
            {
                FelhasznaloID = oktato.Id,
                KulcsID = mesterkulcs.KulcsID,
                ErvenyessegKezdete = DateTime.UtcNow,
                ErvenyessegVege = DateTime.UtcNow.AddDays(30)
            };

            var postResponse = await client.PostAsJsonAsync("/api/mesterkulcsjogosultsag", ujJogosultsag);
            Assert.True(postResponse.IsSuccessStatusCode);

            var jogosultsagok = await client.GetFromJsonAsync<List<JogosultsagListaDto>>("/api/mesterkulcsjogosultsag");
            Assert.NotNull(jogosultsagok);
            Assert.Contains(jogosultsagok, j => j.FelhasznaloID == oktato.Id && j.KulcsID == mesterkulcs.KulcsID);
        }

        [Fact]
        public async Task JogosultsagTorlese_AdminSzerepkorrel_Sikeres()
        {
            var client = _factory.CreateClient();
            var token = await BejelentkezesAsync(client, AdminEmail, SeedPassword);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var felhasznalok = await client.GetFromJsonAsync<List<FelhasznaloDto>>("/api/mesterkulcsjogosultsag/felhasznalok");
            var mesterkulcsok = await client.GetFromJsonAsync<List<MesterkulcsDto>>("/api/mesterkulcsjogosultsag/mesterkulcsok");
            var portas = felhasznalok!.First(f => f.Szerepkor == "Portas");
            var mesterkulcs = mesterkulcsok!.Last();

            var ujJogosultsag = new JogosultsagMentesDto
            {
                FelhasznaloID = portas.Id,
                KulcsID = mesterkulcs.KulcsID,
                ErvenyessegKezdete = DateTime.UtcNow,
                ErvenyessegVege = DateTime.UtcNow.AddDays(10)
            };

            await client.PostAsJsonAsync("/api/mesterkulcsjogosultsag", ujJogosultsag);

            var jogosultsagok = await client.GetFromJsonAsync<List<JogosultsagListaDto>>("/api/mesterkulcsjogosultsag");
            var torlendo = jogosultsagok!.First(j => j.FelhasznaloID == portas.Id && j.KulcsID == mesterkulcs.KulcsID);

            var deleteResponse = await client.DeleteAsync($"/api/mesterkulcsjogosultsag/{torlendo.JogosultsagID}");
            Assert.True(deleteResponse.IsSuccessStatusCode);

            var maradekJogosultsagok = await client.GetFromJsonAsync<List<JogosultsagListaDto>>("/api/mesterkulcsjogosultsag");
            Assert.DoesNotContain(maradekJogosultsagok!, j => j.JogosultsagID == torlendo.JogosultsagID);
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

        private sealed class FelhasznaloDto
        {
            public int Id { get; set; }
            public string Nev { get; set; } = "";
            public string Szerepkor { get; set; } = "";
        }

        private sealed class MesterkulcsDto
        {
            public int KulcsID { get; set; }
        }

        private sealed class JogosultsagMentesDto
        {
            public int FelhasznaloID { get; set; }
            public int KulcsID { get; set; }
            public DateTime ErvenyessegKezdete { get; set; }
            public DateTime ErvenyessegVege { get; set; }
        }

        private sealed class JogosultsagListaDto
        {
            public int JogosultsagID { get; set; }
            public int FelhasznaloID { get; set; }
            public int KulcsID { get; set; }
            public string FelhasznaloNev { get; set; } = "";
            public string Szerepkor { get; set; } = "";
            public DateTime ErvenyessegKezdete { get; set; }
            public DateTime ErvenyessegVege { get; set; }
        }
    }
}
