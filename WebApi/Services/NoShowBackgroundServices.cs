using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

using WebApi.Data;

namespace WebApi.Services
{
    public class NoShowBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(2); // 2 percenként fut

        public NoShowBackgroundService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var mostUtc = DateTime.UtcNow;
                    var tízPercUtan = mostUtc.AddMinutes(-10);

                    // Olyan aktív foglalások, amelyek 10 perce elkezdődtek, de az oktató nem vette fel a kulcsot
                    var lejartFoglalasok = await db.Foglalasok
                        .Where(f => f.Allapot == "Aktiv" && f.KezdoIdo <= tízPercUtan && f.ZaroIdo > mostUtc)
                        .ToListAsync();

                    foreach (var foglalas in lejartFoglalasok)
                    {
                        var kulcsKiadva = await db.Kulcsok.AnyAsync(k => k.TeremID == foglalas.TeremID && k.KiadvaE);

                        if (!kulcsKiadva)
                        {
                            foglalas.Allapot = "MegNemJelent";
                        }
                    }

                    if (lejartFoglalasok.Any())
                    {
                        await db.SaveChangesAsync();
                    }
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }
        }
    }
}