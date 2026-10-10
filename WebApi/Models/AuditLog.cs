using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApi.Models
{
    public class AuditLog
    {
        [Key]
        public int NaploID { get; set; }

        public DateTime Idobelyeg { get; set; } = DateTime.UtcNow;
        public string Muvelet { get; set; } = string.Empty;
        public string Reszletek { get; set; } = string.Empty;

        // Idegen kulcs az Üzemeltetési igazgatóhoz (aki elemzi/kezeli)
        public int UzemeltetesiIgazgatoID { get; set; }
        [ForeignKey("UzemeltetesiIgazgatoID")]
        public Felhasznalo? UzemeltetesiIgazgato { get; set; }

        // Kapcsolatok visszafelé a többi entitáshoz, amik hivatkoznak az audit logra
        public ICollection<KulcsMozgas> KulcsMozgasok { get; set; } = new List<KulcsMozgas>();
        public ICollection<Foglalas> Foglalasok { get; set; } = new List<Foglalas>();
        public ICollection<KarbantartasiIdosav> KarbantartasiIdosavok { get; set; } = new List<KarbantartasiIdosav>();
        public ICollection<MesterkulcsJogosultsag> MesterkulcsJogosultsagok { get; set; } = new List<MesterkulcsJogosultsag>();
    }
}
