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

        // Idegen kulcs
        public int UzemeltetesiIgazgatoID { get; set; }
        [ForeignKey("UzemeltetesiIgazgatoID")]
        public Felhasznalo? UzemeltetesiIgazgato { get; set; }

        public int? FoglalasID { get; set; }
        [ForeignKey("FoglalasID")]
        public Foglalas? Foglalas { get; set; }

        public int? MesterkulcsJogosultsagID { get; set; }
        [ForeignKey("MesterkulcsJogosultsagID")]
        public MesterkulcsJogosultsag? MesterkulcsJogosultsag { get; set; }

        public int? KarbantartasID { get; set; }
        [ForeignKey("KarbantartasID")]
        public KarbantartasiIdosav? KarbantartasiIdosav { get; set; }

        // Kapcsolat
        public KulcsMozgas? KulcsMozgas { get; set; }
        //public ICollection<Foglalas> Foglalasok { get; set; } = new List<Foglalas>();
        //public ICollection<KarbantartasiIdosav> KarbantartasiIdosavok { get; set; } = new List<KarbantartasiIdosav>();
        //public ICollection<MesterkulcsJogosultsag> MesterkulcsJogosultsagok { get; set; } = new List<MesterkulcsJogosultsag>();
    }
}