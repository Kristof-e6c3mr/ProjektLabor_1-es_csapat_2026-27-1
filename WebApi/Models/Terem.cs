using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApi.Models
{
    public class Terem
    {
        [Key]
        public int TeremID { get; set; }

        [Required]
        public string Teremszam { get; set; } = string.Empty;

        public int Ferohely { get; set; }

        public bool Allapot { get; set; } = true;

        // Idegen kulcs az Adminisztrátorhoz (aki a teremtörzset kezeli)
        public int? AdminisztratorID { get; set; }
        [ForeignKey("AdminisztratorID")]
        public Felhasznalo? Adminisztrator { get; set; }

        // Kapcsolatok
        public ICollection<Felszereltseg> Felszereltsegek { get; set; } = new List<Felszereltseg>();
        public ICollection<Foglalas> Foglalasok { get; set; } = new List<Foglalas>();
        public ICollection<KarbantartasiIdosav> KarbantartasiIdosavok { get; set; } = new List<KarbantartasiIdosav>();
        public ICollection<Kulcs> Kulcsok { get; set; } = new List<Kulcs>();
        public ICollection<Hiba> Hibak { get; set; } = new List<Hiba>();
    }
}