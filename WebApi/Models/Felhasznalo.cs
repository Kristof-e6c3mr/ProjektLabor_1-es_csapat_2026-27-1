using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApi.Models
{
    public class Felhasznalo
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Nev { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Jelszo { get; set; } = string.Empty;

        // Szerepkör megkülönböztetéshez (opcionális, de hasznos)
        [Required]
        public string Szerepkor { get; set; } = "Oktato"; // Oktato, Portas, Adminisztrator, UzemeltetesiIgazgato

        public string? PinKod { get; set; }

        // JWT Refresh Tokenhez szükséges mezők
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiryTime { get; set; }

        // Kapcsolatok
        public ICollection<Foglalas> Foglalasok { get; set; } = new List<Foglalas>();
        public ICollection<AuditLog> AuditLogok { get; set; } = new List<AuditLog>();
        public ICollection<Hiba> BejelentettHibak { get; set; } = new List<Hiba>();

        [InverseProperty("Atvevo")]
        public ICollection<KulcsMozgas> AtvettKulcsMozgasok { get; set; } = new List<KulcsMozgas>();

        [InverseProperty("Portas")]
        public ICollection<KulcsMozgas> PortasKulcsMozgasok { get; set; } = new List<KulcsMozgas>();

        public ICollection<MesterkulcsJogosultsag> MesterkulcsJogosultsagok { get; set; } = new List<MesterkulcsJogosultsag>();
        public ICollection<Terem> KezeltTermek { get; set; } = new List<Terem>(); // Adminisztrátorhoz
    }
}