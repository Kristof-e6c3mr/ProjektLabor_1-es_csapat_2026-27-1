using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApi.Models
{
    public class Hiba
    {
        [Key]
        public int HibaID { get; set; }

        public string Leiras { get; set; } = string.Empty;
        public DateTime LetrehozasDatuma { get; set; } = DateTime.UtcNow;
        public bool Allapot { get; set; } = false;

        // Idegen kulcsok
        public int PortasID { get; set; }
        [ForeignKey("PortasID")]
        public Felhasznalo? Portas { get; set; }

        public int TeremID { get; set; }
        [ForeignKey("TeremID")]
        public Terem? Terem { get; set; }
    }
}
