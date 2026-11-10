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
    public class KulcsTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private static readonly string SeedPassword = Environment.GetEnvironmentVariable("SEED_PASSWORD") ?? "Titkos123!";
        private const string AdminEmail = "admin@uni.hu";
        private const string OktatoEmail = "kiss.o@uni.hu";

        public KulcsTests(WebApplicationFactory<Program> factory)
        {
            string dbName = "TestDb_Kulcsok_" + Guid.NewGuid();

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
        public async Task KulcsokLekerdezese_TokenNelkul_401esValasztAd()
        {
            var client = _factory.CreateClient();

            var response = await client.GetAsync("/api/kulcsok");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task KulcsokLekerdezese_AdminSzerepkorrel_SikeresVisszateresEsLista()
        {
            var client = _factory.CreateClient();
            var token = await BejelentkezesAsync(client, AdminEmail, SeedPassword);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync("/api/kulcsok");

            Assert.True(response.IsSuccessStatusCode);
            var kulcsok = await response.Content.ReadFromJsonAsync<List<KulcsListaDto>>();

            Assert.NotNull(kulcsok);
            Assert.NotEmpty(kulcsok);
        }

        [Fact]
        public async Task UjKulcsHozzaadasa_AdminSzerepkorrel_MesterkulcsSikeresenRogzitodik()
        {
            var client = _factory.CreateClient();
            var token = await BejelentkezesAsync(client, AdminEmail, SeedPassword);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var dto = new UjKulcsDto { TeremID = 0 };

            var response = await client.PostAsJsonAsync("/api/kulcsok", dto);

            Assert.True(response.IsSuccessStatusCode);

            var kulcsok = await client.GetFromJsonAsync<List<KulcsListaDto>>("/api/kulcsok");
            Assert.NotNull(kulcsok);
            Assert.Contains(kulcsok, k => k.MesterkulcsE == true);
        }

        [Fact]
        public async Task UjKulcsHozzaadasa_OktatoSzerepkorrel_403asValasztAd()
        {
            var client = _factory.CreateClient();
            var token = await BejelentkezesAsync(client, OktatoEmail, SeedPassword);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var dto = new UjKulcsDto { TeremID = 0 };

            var response = await client.PostAsJsonAsync("/api/kulcsok", dto);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task KulcsTorlese_AdminSzerepkorrel_SikeresenTorol()
        {
            var client = _factory.CreateClient();
            var token = await BejelentkezesAsync(client, AdminEmail, SeedPassword);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            await client.PostAsJsonAsync("/api/kulcsok", new UjKulcsDto { TeremID = 0 });

            var kulcsok = await client.GetFromJsonAsync<List<KulcsListaDto>>("/api/kulcsok");
            Assert.NotNull(kulcsok);
            var torlendoKulcs = kulcsok.Last(k => k.MesterkulcsE == true);

            var deleteResponse = await client.DeleteAsync($"/api/kulcsok/{torlendoKulcs.KulcsID}");
            Assert.True(deleteResponse.IsSuccessStatusCode);

            var maradekKulcsok = await client.GetFromJsonAsync<List<KulcsListaDto>>("/api/kulcsok");
            Assert.DoesNotContain(maradekKulcsok!, k => k.KulcsID == torlendoKulcs.KulcsID);
        }

        [Fact]
        public async Task KulcsTorlese_OktatoSzerepkorrel_403asValasztAd()
        {
            var client = _factory.CreateClient();
            var token = await BejelentkezesAsync(client, OktatoEmail, SeedPassword);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.DeleteAsync("/api/kulcsok/1");

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

        private sealed class KulcsListaDto
        {
            public int KulcsID { get; set; }
            public bool MesterkulcsE { get; set; }
            public bool KiadvaE { get; set; }
            public string Teremszam { get; set; } = string.Empty;
        }

        private sealed class UjKulcsDto
        {
            public int TeremID { get; set; }
        }
    }
}