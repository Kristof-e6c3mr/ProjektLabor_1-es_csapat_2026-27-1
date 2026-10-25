using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using WebApi.Data; // Ellenőrizd, hogy ez a jó namespace a DbContext-hez!
using WebApi.Models;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HibakController : ControllerBase
    {
        private readonly AppDbContext _context;

        public HibakController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/hibak/termek
        // Lekéri a termeket a frontend lenyíló listájához
        [HttpGet("termek")]
        public async Task<IActionResult> GetTermek()
        {
            var termek = await _context.Termek
                .Select(t => new { t.TeremID, t.Teremszam })
                .ToListAsync();

            return Ok(termek);
        }

        // POST: api/hibak
        // Új hibajegy mentése
        [HttpPost]
        public async Task<IActionResult> CreateHiba([FromBody] HibaCreateDto dto)
        {
            var hiba = new Hiba
            {
                Leiras = dto.Leiras,
                TeremID = dto.TeremID,
                PortasID = dto.PortasID, // Később ezt a bejelentkezett felhasználóból vesszük
                LetrehozasDatuma = DateTime.UtcNow,
                Allapot = false
            };

            _context.Hibak.Add(hiba);
            await _context.SaveChangesAsync();

            return Ok(hiba);
        }
    }

    // Segédosztály az adatok fogadásához
    public class HibaCreateDto
    {
        public string Leiras { get; set; } = string.Empty;
        public int TeremID { get; set; }
        public int PortasID { get; set; }
    }
}