using Microsoft.AspNetCore.Identity.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WebApi.Data;    // AppDbContext
using WebApi.Models;  // Tables

namespace WebApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            //configure DBcontext to use Postgre SQL
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

            // Add services to the container.
            builder.Services.AddControllers();

            builder.Services.ConfigureHttpJsonOptions(options =>
            {
                options.SerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
            });

            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.MapScalarApiReference();
            }
            if (!app.Environment.IsDevelopment())
            {
                app.UseHttpsRedirection();
            }

            app.UseAuthorization();

            app.MapControllers();

            // --- ADATBÁZIS INICIALIZÁLÁSA ÉS TESZTADATOK FELTÖLTÉSE ---
            using (var scope = app.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<WebApi.Data.AppDbContext>();
                // Ellenőrzi, hogy van-e adatbázis, ha nincs létrehozza a migrációk alapján
                context.Database.EnsureCreated();
                // Feltölti a tesztadatokkal
                WebApi.Data.DbInitializer.Initialize(context);
            }

            // POST Endpoint a Login Requestre
            app.MapPost("/api/login", async (LoginModel request, AppDbContext db, IConfiguration config) =>
            {
                var user = await db.Felhasznalok
                    .FirstOrDefaultAsync(f => f.Nev == request.Username || f.Email == request.Username);

                if (user != null && user.Jelszo == request.Password)
                {
                    // Create the security key
                    var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["JwtSettings:Key"]!));
                    var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

                    // Define the payload
                    var claims = new[]
                    {
                        new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                        new Claim(JwtRegisteredClaimNames.Email, user.Email),
                        new Claim(ClaimTypes.NameIdentifier, user.Nev),
                        new Claim(ClaimTypes.Role, user.Szerepkor)
                    };

                    // 3. Assemble the token
                    var token = new JwtSecurityToken(
                        issuer: config["JwtSettings:Issuer"],
                        audience: config["JwtSettings:Audience"],
                        claims: claims,
                        expires: DateTime.UtcNow.AddMinutes(15),
                        signingCredentials: credentials);

                    // 4. Serialize to a string
                    var jwtString = new JwtSecurityTokenHandler().WriteToken(token);

                    return Results.Ok(new
                    {
                        Token = jwtString,
                        Role = user.Szerepkor
                    });
                }

                return Results.Unauthorized();
            })
            .WithName("Login");

            // D pont: Szolgáltatás állapotjelző (Health Check) végpont
            app.MapGet("/api/health", () => Results.Ok(new { status = "Healthy", timestamp = DateTime.UtcNow })).WithName("HealthCheck");

            // Alapvető adat-lekérdező végpont (felhasználók listázása)
            app.MapGet("/api/felhasznalok", async (AppDbContext db) =>
            {
                // JAVÍTÁS: Projektálás a végtelen ciklus elkerülésére a séma generálásakor
                return await db.Felhasznalok
                    .Select(f => new { f.Id, f.Nev, f.Email, f.Szerepkor })
                    .ToListAsync();
            }).WithName("GetFelhasznalok");

            app.Run();
        }
    }
}