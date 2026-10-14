using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApi.Models
{
    public class Foglalas
    {
        [Key]
        public int FoglalasID { get; set; }

        public DateTime KezdoIdo { get; set; }
        public DateTime ZaroIdo { get; set; }
        
        public string Allapot { get; set; } = "Aktiv";

        // Idegen kulcsok
        public int OktatoID { get; set; }
        [ForeignKey("OktatoID")]
        public Felhasznalo? Oktato { get; set; }

        public int TeremID { get; set; }
        [ForeignKey("TeremID")]
        public Terem? Terem { get; set; }

        //public int AuditLogID { get; set; }
        //[ForeignKey("AuditLogID")]
        //public AuditLog? AuditLog { get; set; }
        public ICollection<AuditLog> AuditLogok { get; set; } = new List<AuditLog>();

        // Kapcsolat
        public ICollection<Felszereltseg> IgenyeltFelszereltsegek { get; set; } = new List<Felszereltseg>();
    }
}
