using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using WebApi.Data;
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

        // Portás és Admin számára is elérhető termek lekérése a hiba bejelentéshez / szűréshez
        [HttpGet("termek")]
        [Authorize(Roles = "Portas,Adminisztrator")]
        public async Task<IActionResult> GetTermek()
        {
            var termek = await _context.Termek
                .Select(t => new { t.TeremID, t.Teremszam })
                .ToListAsync();

            return Ok(termek);
        }

        // KIZÁRÓLAG AZ ADMINISZTRÁTOR LISTÁZHATJÁK A HIBÁKAT
        [HttpGet]
        [Authorize(Roles = "Adminisztrator")]
        public async Task<IActionResult> GetHibak()
        {
            var hibak = await _context.Hibak
                .Include(h => h.Terem)
                .Include(h => h.Portas)
                .Select(h => new
                {
                    h.HibaID,
                    Teremszam = h.Terem != null ? h.Terem.Teremszam : "-",
                    PortasNev = h.Portas != null ? h.Portas.Nev : "-",
                    h.Leiras,
                    h.LetrehozasDatuma,
                    h.Allapot // true = javítva, false = folyamatban / bejelentve
                })
                .OrderByDescending(h => h.LetrehozasDatuma)
                .ToListAsync();

            return Ok(hibak);
        }

        // KIZÁRÓLAG A PORTÁS TUD ÚJ HIBÁT FELVINNI
        [HttpPost]
        [Authorize(Roles = "Portas")]
        public async Task<IActionResult> CreateHiba([FromBody] HibaCreateDto dto)
        {
            var hiba = new Hiba
            {
                Leiras = dto.Leiras,
                TeremID = dto.TeremID,
                PortasID = dto.PortasID,
                LetrehozasDatuma = DateTime.UtcNow,
                Allapot = false // Alapból nem javított
            };

            _context.Hibak.Add(hiba);
            await _context.SaveChangesAsync();

            return Ok(hiba);
        }

        // KIZÁRÓLAG AZ ADMINISZTRÁTOR MÓDOSÍTHATJA A HIBA ÁLLAPOTÁT
        [HttpPut("{id}/statusz")]
        [Authorize(Roles = "Adminisztrator")]
        public async Task<IActionResult> UpdateHibaStatusz(int id, [FromBody] HibaStatuszDto dto)
        {
            var hiba = await _context.Hibak.FindAsync(id);
            if (hiba == null) return NotFound("A hibajegy nem található.");

            hiba.Allapot = dto.Allapot;
            await _context.SaveChangesAsync();

            return Ok(hiba);
        }

        // KIZÁRÓLAG AZ ADMINISZTRÁTOR TÖRLETHETI A HIBÁT
        [HttpDelete("{id}")]
        [Authorize(Roles = "Adminisztrator")]
        public async Task<IActionResult> DeleteHiba(int id)
        {
            var hiba = await _context.Hibak.FindAsync(id);
            if (hiba == null) return NotFound("A hibajegy nem található.");

            _context.Hibak.Remove(hiba);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }

    public class HibaCreateDto
    {
        public string Leiras { get; set; } = string.Empty;
        public int TeremID { get; set; }
        public int PortasID { get; set; }
    }

    public class HibaStatuszDto
    {
        public bool Allapot { get; set; }
    }
}