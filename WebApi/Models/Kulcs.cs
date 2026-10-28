using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

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
        [JsonIgnore]
        public Terem? Terem { get; set; }

        // Kapcsolatok
        [JsonIgnore]
        public ICollection<KulcsMozgas> KulcsMozgasok { get; set; } = new List<KulcsMozgas>();

        [JsonIgnore]
        public ICollection<MesterkulcsJogosultsag> MesterkulcsJogosultsagok { get; set; } = new List<MesterkulcsJogosultsag>();
    }
}