using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
        public Kulcs? Kulcs { get; set; }
        
        public int AtvevoID { get; set; }
        [ForeignKey("AtvevoID")]
        public Felhasznalo? Atvevo { get; set; }
        
        public int PortasID { get; set; }
        [ForeignKey("PortasID")]
        public Felhasznalo? Portas { get; set; }

        public int AuditLogID { get; set; }
        [ForeignKey("AuditLogID")]
        public AuditLog? AuditLog { get; set; }
    }
}
