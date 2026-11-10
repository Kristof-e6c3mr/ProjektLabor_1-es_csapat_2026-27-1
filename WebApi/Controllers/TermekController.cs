using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using WebApi.Data;
using WebApi.Models;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
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
                    Felszereltsegek = t.Felszereltsegek != null
                        ? t.Felszereltsegek.Select(f => f.Nev).ToList()
                        : new List<string>()
                })
                .ToListAsync();

            return Ok(termek);
        }

        [HttpPost]
        [Authorize(Roles = "Adminisztrator")]
        public async Task<IActionResult> CreateTerem([FromBody] TeremInputDto dto)
        {
            if (dto.Ferohely < 0) return BadRequest("A férőhely nem lehet negatív!");

            bool letezik = await _context.Termek.AnyAsync(t => t.Teremszam == dto.Teremszam);
            if (letezik) return BadRequest("Ez a teremszám már létezik!");

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

            // Audit log rögzítés olvasható formában
            var adminId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            _context.AuditLogok.Add(new AuditLog
            {
                Muvelet = "Terem Felvétele",
                Reszletek = $"Új terem került rögzítésre: {dto.Teremszam} számú, {dto.Ferohely} férőhelyes terem.",
                UzemeltetesiIgazgatoID = adminId > 0 ? adminId : 1,
                Idobelyeg = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            return Ok(terem);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Adminisztrator")]
        public async Task<IActionResult> UpdateTerem(int id, [FromBody] TeremInputDto dto)
        {
            var terem = await _context.Termek
                .Include(t => t.Felszereltsegek)
                .FirstOrDefaultAsync(t => t.TeremID == id);

            if (terem == null) return NotFound("A terem nem található.");
            if (dto.Ferohely < 0) return BadRequest("A férőhely nem lehet negatív!");

            string regiTeremszam = terem.Teremszam;
            terem.Teremszam = dto.Teremszam;
            terem.Ferohely = dto.Ferohely;
            terem.Allapot = dto.Allapot;

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

            var adminId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            _context.AuditLogok.Add(new AuditLog
            {
                Muvelet = "Terem Módosítása",
                Reszletek = $"A(z) {regiTeremszam} számú terem adatai frissítve lettek (Új teremszám: {dto.Teremszam}, {dto.Ferohely} férőhely).",
                UzemeltetesiIgazgatoID = adminId > 0 ? adminId : 1,
                Idobelyeg = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            return Ok(terem);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Adminisztrator")]
        public async Task<IActionResult> DeleteTerem(int id)
        {
            var terem = await _context.Termek
                .Include(t => t.Felszereltsegek)
                .Include(t => t.Kulcsok)
                .Include(t => t.Foglalasok)
                .Include(t => t.KarbantartasiIdosavok)
                .Include(t => t.Hibak)
                .FirstOrDefaultAsync(t => t.TeremID == id);

            if (terem == null) return NotFound();

            if (terem.Kulcsok.Any()) return BadRequest("A terem nem törölhető, mert tartozik hozzá kulcs!");
            if (terem.Foglalasok.Any()) return BadRequest("A terem nem törölhető, mert vannak hozzá rögzített foglalások!");
            if (terem.KarbantartasiIdosavok.Any()) return BadRequest("A terem nem törölhető, mert tartozik hozzá karbantartási idősáv!");
            if (terem.Hibak.Any()) return BadRequest("A terem nem törölhető, mert be van jelentve hozzá hiba!");

            string teremszam = terem.Teremszam;
            int ferohely = terem.Ferohely;
            terem.Felszereltsegek.Clear();
            _context.Termek.Remove(terem);

            var adminId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            _context.AuditLogok.Add(new AuditLog
            {
                Muvelet = "Terem Törlése",
                Reszletek = $"Terem törölve: {teremszam} számú ({ferohely} férőhelyes) terem.",
                UzemeltetesiIgazgatoID = adminId > 0 ? adminId : 1,
                Idobelyeg = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            return NoContent();
        }
    }

    public class TeremInputDto
    {
        public string Teremszam { get; set; } = "";
        public int Ferohely { get; set; }
        public bool Allapot { get; set; } = true;
        public List<int> FelszereltsegIDs { get; set; } = new();
    }
}