using System.ComponentModel.DataAnnotations;

namespace WebApi.Models
{
    public class Felszereltseg
    {
        [Key]
        public int FelszereltsegID { get; set; }

        [Required]
        public string Nev { get; set; } = string.Empty;

        // Kapcsolatok
        public ICollection<Terem> Termek { get; set; } = new List<Terem>();
        public ICollection<Foglalas> Foglalasok { get; set; } = new List<Foglalas>();
    }
}