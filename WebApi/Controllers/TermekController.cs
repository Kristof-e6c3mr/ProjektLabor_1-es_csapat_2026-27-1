using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using WebApi.Data;
using WebApi.Models;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Bárki, aki be van jelentkezve, olvashatja a termeket[cite: 46]
    public class TermekController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TermekController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetTermek()
        {
            var termek = await _context.Termek
                .Include(t => t.Felszereltsegek)
                .Select(t => new
                {
                    TeremID = t.TeremID,
                    Teremszam = t.Teremszam,
                    Ferohely = t.Ferohely,
                    Allapot = t.Allapot,
                    // Itt megint sima string listaként adjuk vissza a neveket, 
                    // hogy a Foglalasok.razor-ban lévő szűrések ne törjenek el!
                    Felszereltsegek = t.Felszereltsegek != null
                        ? t.Felszereltsegek.Select(f => f.Nev).ToList()
                        : new List<string>()
                })
                .ToListAsync();

            return Ok(termek);
        }

        [HttpPost]
        [Authorize(Roles = "Adminisztrator")] // Csak admin rögzíthet[cite: 46]
        public async Task<IActionResult> CreateTerem([FromBody] TeremInputDto dto)
        {
            if (dto.Ferohely < 0)
            {
                return BadRequest("A férőhely nem lehet negatív!");
            }

            bool letezik = await _context.Termek.AnyAsync(t => t.Teremszam == dto.Teremszam);
            if (letezik)
            {
                return BadRequest("Ez a teremszám már létezik!");
            }

            var terem = new Terem
            {
                Teremszam = dto.Teremszam,
                Ferohely = dto.Ferohely,
                Allapot = dto.Allapot
            };

            if (dto.FelszereltsegIDs != null && dto.FelszereltsegIDs.Any())
            {
                terem.Felszereltsegek = await _context.Set<Felszereltseg>()
                    .Where(f => dto.FelszereltsegIDs.Contains(f.FelszereltsegID))
                    .ToListAsync();
            }

            _context.Termek.Add(terem);
            await _context.SaveChangesAsync();
            return Ok(terem);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Adminisztrator")] // Csak admin módosíthat
        public async Task<IActionResult> UpdateTerem(int id, [FromBody] TeremInputDto dto)
        {
            var terem = await _context.Termek
                .Include(t => t.Felszereltsegek)
                .FirstOrDefaultAsync(t => t.TeremID == id);

            if (terem == null) return NotFound("A terem nem található.");

            if (dto.Ferohely < 0)
            {
                return BadRequest("A férőhely nem lehet negatív!");
            }

            // Megjegyzés: A teremszámot és férőhelyet a frontend oldalon letiltottuk, 
            // de itt biztonság kedvéért frissítjük vagy hagyjuk.
            terem.Teremszam = dto.Teremszam;
            terem.Ferohely = dto.Ferohely;
            terem.Allapot = dto.Allapot;

            // Felszereltségek frissítése a pipák (ID-k) alapján, amit az admin beállított
            terem.Felszereltsegek.Clear();
            if (dto.FelszereltsegIDs != null && dto.FelszereltsegIDs.Any())
            {
                var kivalasztottFelszereltsegek = await _context.Set<Felszereltseg>()
                    .Where(f => dto.FelszereltsegIDs.Contains(f.FelszereltsegID))
                    .ToListAsync();

                foreach (var f in kivalasztottFelszereltsegek)
                {
                    terem.Felszereltsegek.Add(f);
                }
            }

            await _context.SaveChangesAsync();
            return Ok(terem);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Adminisztrator")] // Csak admin törölhet[cite: 46]
        public async Task<IActionResult> DeleteTerem(int id)
        {
            var terem = await _context.Termek.FindAsync(id);
            if (terem == null) return NotFound();

            _context.Termek.Remove(terem);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }

    // Ez az egyetlen segédosztály, ami a frontendről jövő adatokat fogadja (Admin felület)
    public class TeremInputDto
    {
        public string Teremszam { get; set; } = "";
        public int Ferohely { get; set; }
        public bool Allapot { get; set; } = true;
        public List<int> FelszereltsegIDs { get; set; } = new();
    }
}