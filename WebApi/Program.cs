using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

using Scalar.AspNetCore;

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
            builder.Services.AddControllers().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
            });
            builder.Services.AddEndpointsApiExplorer();

            builder.Services.ConfigureHttpJsonOptions(options =>
            {
                options.SerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
            });

            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            // --- JWT HITELESÍTÉS BEÁLLÍTÁSA ---
            // A kulcsnak pontosan egyeznie kell a LoginControllerben lévővel!
            var key = Encoding.ASCII.GetBytes("NagyonTitkosKulcsAmiLegalabb32KarakterHosszu!");

            builder.Services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(x =>
            {
                x.RequireHttpsMetadata = false; // Docker hálózaton HTTP van
                x.SaveToken = true;
                x.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false, // Fejlesztéshez kikapcsolva
                    ValidateAudience = false, // Fejlesztéshez kikapcsolva
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            });

            // Autorizáció regisztrálása
            builder.Services.AddAuthorization();

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

            // --- A SORREND KRITIKUS: A UseAuthentication-nek a UseAuthorization ELŐTT kell lennie ---
            app.UseAuthentication();
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

            // TÖRÖLTÜK a Minimal API MapGet("/api/login") részt. 
            // A bejelentkezést mostantól a valós adatbázist használó LoginController kezeli!

            // Szolgáltatás állapotjelző (Health Check) végpont
            app.MapGet("/api/health", () => Results.Ok(new { status = "Healthy", timestamp = DateTime.UtcNow })).WithName("HealthCheck");

            // Alapvető adat-lekérdező végpont (felhasználók listázása)
            app.MapGet("/api/felhasznalok", async (AppDbContext db) =>
            {
                return await db.Felhasznalok
                    .Select(f => new { f.Id, f.Nev, f.Email, f.Szerepkor })
                    .ToListAsync();
            }).WithName("GetFelhasznalok");

            app.Run();
        }
    }
}