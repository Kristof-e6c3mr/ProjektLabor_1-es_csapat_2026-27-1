using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using WebApi.Data;
using WebApi.Models;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Adminisztrator")]
    public class KulcsokController : ControllerBase
    {
        private readonly AppDbContext _context;

        public KulcsokController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetKulcsok()
        {
            var kulcsok = await _context.Kulcsok
                .Include(k => k.Terem)
                .Select(k => new
                {
                    k.KulcsID,
                    k.MesterkulcsE,
                    k.KiadvaE,
                    k.TeremID,
                    Teremszam = k.Terem != null ? k.Terem.Teremszam : "-"
                })
                .ToListAsync();

            return Ok(kulcsok);
        }

        [HttpPost]
        public async Task<IActionResult> CreateKulcs([FromBody] UjKulcsDto dto)
        {
            string kulcsMegnevezes = "Mesterkulcs";

            if (dto.TeremID > 0)
            {
                bool vanMarKulcs = await _context.Kulcsok
                    .AnyAsync(k => k.TeremID == dto.TeremID && !k.MesterkulcsE);

                if (vanMarKulcs)
                {
                    return BadRequest("Ehhez a teremhez már tartozik kulcs! Teremenként csak egy kulcs rögzíthető.");
                }

                var terem = await _context.Termek.FindAsync(dto.TeremID);
                kulcsMegnevezes = $"Teremkulcs ({terem?.Teremszam ?? "Ismeretlen terem"})";
            }

            var kulcs = new Kulcs
            {
                MesterkulcsE = dto.TeremID == 0,
                KiadvaE = false,
                TeremID = dto.TeremID == 0 ? null : dto.TeremID
            };

            _context.Kulcsok.Add(kulcs);

            var adminId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "1");
            _context.AuditLogok.Add(new AuditLog
            {
                Muvelet = "Kulcs Felvétele",
                Reszletek = $"Új kulcs felvétele a rendszerbe: {kulcsMegnevezes}.",
                UzemeltetesiIgazgatoID = adminId,
                Idobelyeg = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            return Ok(kulcs);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteKulcs(int id)
        {
            var kulcs = await _context.Kulcsok
                .Include(k => k.Terem)
                .FirstOrDefaultAsync(k => k.KulcsID == id);

            if (kulcs == null) return NotFound();

            if (kulcs.KiadvaE)
            {
                return BadRequest("A kulcs jelenleg ki van adva, ezért nem törölhető!");
            }

            string kulcsMegnevezes = kulcs.MesterkulcsE ? "Mesterkulcs" : $"Teremkulcs ({kulcs.Terem?.Teremszam})";
            _context.Kulcsok.Remove(kulcs);

            var adminId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "1");
            _context.AuditLogok.Add(new AuditLog
            {
                Muvelet = "Kulcs Törlése",
                Reszletek = $"Kulcs törlése a rendszerből: {kulcsMegnevezes}.",
                UzemeltetesiIgazgatoID = adminId,
                Idobelyeg = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            return NoContent();
        }
    }

    public class UjKulcsDto
    {
        public int TeremID { get; set; }
    }
}