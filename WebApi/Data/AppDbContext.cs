using System.Reflection.Emit;

using Microsoft.EntityFrameworkCore;

using WebApi.Models;

namespace WebApi.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Felhasznalo> Felhasznalok { get; set; }
        public DbSet<Terem> Termek { get; set; }
        public DbSet<Kulcs> Kulcsok { get; set; }
        public DbSet<KulcsMozgas> KulcsMozgasok { get; set; }
        public DbSet<Foglalas> Foglalasok { get; set; }
        public DbSet<KarbantartasiIdosav> KarbantartasiIdosavok { get; set; }
        public DbSet<AuditLog> AuditLogok { get; set; }
        public DbSet<Hiba> Hibak { get; set; }
        public DbSet<MesterkulcsJogosultsag> MesterkulcsJogosultsagok { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // --- KULCSMOZGÁS KAPCSOLATOK PONTOSÍTÁSA ---
            modelBuilder.Entity<KulcsMozgas>()
                .HasOne(km => km.Atvevo)
                .WithMany(f => f.AtvettKulcsMozgasok)
                .HasForeignKey(km => km.AtvevoID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<KulcsMozgas>()
                .HasOne(km => km.Portas)
                .WithMany(f => f.PortasKulcsMozgasok)
                .HasForeignKey(km => km.PortasID)
                .OnDelete(DeleteBehavior.Restrict);

            // Biztosítjuk, hogy ne legyen többszörös cascade delete hiba körkörös kapcsolatoknál
            foreach (var relationship in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
            {
                relationship.DeleteBehavior = DeleteBehavior.Restrict;
            }
        }
    }
}