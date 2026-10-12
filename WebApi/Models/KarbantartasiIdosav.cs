using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApi.Models
{
    public class KarbantartasiIdosav
    {
        [Key]
        public int KarbantartasID { get; set; }

        public DateTime KezdoIdo { get; set; }
        public DateTime ZaroIdo { get; set; }

        public string Leiras { get; set; } = string.Empty;

        // Idegen kulcsok
        public int TeremID { get; set; }
        [ForeignKey("TeremID")]
        public Terem? Terem { get; set; }

        //public int AuditLogID { get; set; }
        //[ForeignKey("AuditLogID")]
        //public AuditLog? AuditLog { get; set; }
        public ICollection<AuditLog> AuditLogok { get; set; } = new List<AuditLog>();
    }
}
