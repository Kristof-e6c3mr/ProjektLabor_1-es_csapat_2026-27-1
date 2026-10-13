
using Scalar.AspNetCore;
using Microsoft.EntityFrameworkCore;
using WebApi.Data;
using Microsoft.AspNetCore.Identity.Data;

namespace WebApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.MapScalarApiReference();
            }

            app.UseHttpsRedirection();

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
            app.MapPost("/api/login", (LoginModel request) =>
            {
                //static creds for testing
                if (request.Username == "asd" && request.Password == "123")
                {
                    return Results.Ok(new { Token = "XD" });
                }

                return Results.Unauthorized();
            }
            ).WithName("Login").WithOpenApi();

            app.Run();
        }
    }
}
