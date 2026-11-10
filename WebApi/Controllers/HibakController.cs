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

        [HttpGet("termek")]
        [Authorize(Roles = "Portas,Adminisztrator")]
        public async Task<IActionResult> GetTermek()
        {
            var termek = await _context.Termek
                .Select(t => new { t.TeremID, t.Teremszam })
                .ToListAsync();

            return Ok(termek);
        }

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
                    h.Allapot
                })
                .OrderByDescending(h => h.LetrehozasDatuma)
                .ToListAsync();

            return Ok(hibak);
        }

        [HttpPost]
        [Authorize(Roles = "Portas")]
        public async Task<IActionResult> CreateHiba([FromBody] HibaCreateDto dto)
        {
            var terem = await _context.Termek.FindAsync(dto.TeremID);
            var portas = await _context.Felhasznalok.FindAsync(dto.PortasID);
            string teremszam = terem?.Teremszam ?? "Ismeretlen";
            string portasNev = portas?.Nev ?? "Ismeretlen";

            var hiba = new Hiba
            {
                Leiras = dto.Leiras,
                TeremID = dto.TeremID,
                PortasID = dto.PortasID,
                LetrehozasDatuma = DateTime.UtcNow,
                Allapot = false
            };

            _context.Hibak.Add(hiba);

            _context.AuditLogok.Add(new AuditLog
            {
                Muvelet = "Hibajegy Bejelentése",
                Reszletek = $"Hiba bejelentve ({teremszam}): {dto.Leiras} (Bejelentette: {portasNev} portás).",
                UzemeltetesiIgazgatoID = dto.PortasID,
                Idobelyeg = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            return Ok(hiba);
        }

        [HttpPut("{id}/statusz")]
        [Authorize(Roles = "Adminisztrator")]
        public async Task<IActionResult> UpdateHibaStatusz(int id, [FromBody] HibaStatuszDto dto)
        {
            var hiba = await _context.Hibak
                .Include(h => h.Terem)
                .FirstOrDefaultAsync(h => h.HibaID == id);

            if (hiba == null) return NotFound("A hibajegy nem található.");

            string teremszam = hiba.Terem?.Teremszam ?? "Ismeretlen";
            hiba.Allapot = dto.Allapot;

            var adminId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "1");
            _context.AuditLogok.Add(new AuditLog
            {
                Muvelet = "Hibajegy Státusz Módosítás",
                Reszletek = $"A(z) {teremszam} terembe bejelentett hibajegy állapota megváltozott: {(dto.Allapot ? "Javítva" : "Folyamatban")}.",
                UzemeltetesiIgazgatoID = adminId,
                Idobelyeg = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            return Ok(hiba);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Adminisztrator")]
        public async Task<IActionResult> DeleteHiba(int id)
        {
            var hiba = await _context.Hibak
                .Include(h => h.Terem)
                .FirstOrDefaultAsync(h => h.HibaID == id);

            if (hiba == null) return NotFound("A hibajegy nem található.");

            string teremszam = hiba.Terem?.Teremszam ?? "Ismeretlen";
            string leiras = hiba.Leiras;
            _context.Hibak.Remove(hiba);

            var adminId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "1");
            _context.AuditLogok.Add(new AuditLog
            {
                Muvelet = "Hibajegy Törlése",
                Reszletek = $"Hibajegy törölve a(z) {teremszam} teremhez rögzítve ({leiras}).",
                UzemeltetesiIgazgatoID = adminId,
                Idobelyeg = DateTime.UtcNow
            });

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