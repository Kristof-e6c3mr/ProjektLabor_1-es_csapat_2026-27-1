using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using WebApi.Data;
using WebApi.Models;

namespace WebApi.Services
{
    public class NoShowBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(2); // 2 percenként fut
        private readonly TimeSpan _noShowDelay;

        public NoShowBackgroundService(IServiceProvider serviceProvider, IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _noShowDelay = TimeSpan.FromMinutes(configuration.GetValue("NoShow:DelayMinutes", 10));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var mostUtc = DateTime.UtcNow;
                    var hataridoUtc = mostUtc - _noShowDelay;

                    // Olyan aktív foglalások, amelyek a beállított időn belül elkezdődtek, de az oktató nem vette fel a kulcsot
                    var lejartFoglalasok = await db.Foglalasok
                        .Include(f => f.Terem)
                        .Include(f => f.Oktato)
                        .Where(f => f.Allapot == "Aktiv" && f.KezdoIdo <= hataridoUtc && f.ZaroIdo > mostUtc)
                        .ToListAsync();

                    var uzemeltetoId = await db.Felhasznalok
                        .Where(f => f.Szerepkor == "UzemeltetesiIgazgato")
                        .Select(f => f.Id)
                        .FirstOrDefaultAsync();

                    foreach (var foglalas in lejartFoglalasok)
                    {
                        var kulcsKiadva = await db.Kulcsok.AnyAsync(k => k.TeremID == foglalas.TeremID && k.KiadvaE);

                        if (!kulcsKiadva)
                        {
                            foglalas.Allapot = "MegNemJelent";

                            var oktatoNev = foglalas.Oktato?.Nev ?? "Ismeretlen oktató";
                            var teremszam = foglalas.Terem?.Teremszam ?? "Ismeretlen terem";
                            var kezdoHelyi = foglalas.KezdoIdo.ToLocalTime();
                            var zaroHelyi = foglalas.ZaroIdo.ToLocalTime();

                            db.AuditLogok.Add(new AuditLog
                            {
                                Muvelet = "No-show Foglalás",
                                Reszletek = $"No-show: {oktatoNev} nem vette fel a kulcsot a(z) {teremszam} teremre ({kezdoHelyi:yyyy.MM.dd HH:mm}-{zaroHelyi:HH:mm}). A foglalás 'MegNemJelent' státuszba került, a terem felszabadult.",
                                UzemeltetesiIgazgatoID = uzemeltetoId != 0 ? uzemeltetoId : foglalas.OktatoID,
                                FoglalasID = foglalas.FoglalasID,
                                Idobelyeg = mostUtc
                            });
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