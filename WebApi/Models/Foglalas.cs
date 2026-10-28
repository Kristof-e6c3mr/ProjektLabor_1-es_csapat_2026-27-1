using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

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
        [JsonIgnore]
        public Felhasznalo? Oktato { get; set; }

        public int TeremID { get; set; }

        [ForeignKey("TeremID")]
        [JsonIgnore]
        public Terem? Terem { get; set; }

        [JsonIgnore]
        public ICollection<AuditLog> AuditLogok { get; set; } = new List<AuditLog>();

        // Kapcsolat
        [JsonIgnore]
        public ICollection<Felszereltseg> IgenyeltFelszereltsegek { get; set; } = new List<Felszereltseg>();
    }
}