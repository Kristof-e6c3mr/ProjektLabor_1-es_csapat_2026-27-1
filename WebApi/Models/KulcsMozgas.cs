using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace WebApi.Models
{
    public class KulcsMozgas
    {
        [Key]
        public int MozgasID { get; set; }

        public DateTime Idobelyeg { get; set; } = DateTime.UtcNow;

        // Idegen kulcsok
        public int KulcsID { get; set; }

        [ForeignKey("KulcsID")]
        [JsonIgnore]
        public Kulcs? Kulcs { get; set; }

        public int AtvevoID { get; set; }

        [ForeignKey("AtvevoID")]
        [JsonIgnore]
        public Felhasznalo? Atvevo { get; set; }

        public int PortasID { get; set; }

        [ForeignKey("PortasID")]
        [JsonIgnore]
        public Felhasznalo? Portas { get; set; }

        public int AuditLogID { get; set; }

        [ForeignKey("AuditLogID")]
        [JsonIgnore]
        public AuditLog? AuditLog { get; set; }
    }
}