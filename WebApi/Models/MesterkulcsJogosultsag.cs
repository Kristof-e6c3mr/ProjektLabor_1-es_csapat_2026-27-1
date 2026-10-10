using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApi.Models
{
    public class MesterkulcsJogosultsag
    {
        [Key]
        public int JogosultsagID { get; set; }

        public DateTime ErvenyessegKezdete { get; set; }
        public DateTime ErvenyessegVege { get; set; }

        // Idegen kulcsok
        public int AdminisztratorID { get; set; }
        [ForeignKey("AdminisztratorID")]
        public Felhasznalo? Adminisztrator { get; set; }

        public int KulcsID { get; set; }
        [ForeignKey("KulcsID")]
        public Kulcs? Kulcs { get; set; }

        public int AuditLogID { get; set; }
        [ForeignKey("AuditLogID")]
        public AuditLog? AuditLog { get; set; }
    }
}
