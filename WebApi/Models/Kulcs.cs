using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApi.Models
{
    public class Kulcs
    {
        [Key]
        public int KulcsID { get; set; }

        public bool MesterkulcsE { get; set; }

        public bool KiadvaE { get; set; }

        // Idegen kulcs a Teremhez
        public int? TeremID { get; set; }
        [ForeignKey("TeremID")]
        public Terem? Terem { get; set; }

        // Kapcsolatok
        public ICollection<KulcsMozgas> KulcsMozgasok { get; set; } = new List<KulcsMozgas>();
        public ICollection<MesterkulcsJogosultsag> MesterkulcsJogosultsagok { get; set; } = new List<MesterkulcsJogosultsag>();
    }
}