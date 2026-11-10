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
    public class FelszereltsegController : ControllerBase
    {
        private readonly AppDbContext _context;

        public FelszereltsegController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetFelszereltsegek()
        {
            var lista = await _context.Set<Felszereltseg>()
                .Select(f => new { f.FelszereltsegID, f.Nev })
                .OrderBy(f => f.Nev)
                .ToListAsync();

            return Ok(lista);
        }

        [HttpPost]
        [Authorize(Roles = "Adminisztrator")]
        public async Task<IActionResult> UjFelszereltseg([FromBody] FelszereltsegInputDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Nev))
                return BadRequest("A felszereltség neve nem lehet üres!");

            bool letezik = await _context.Set<Felszereltseg>().AnyAsync(f => f.Nev.ToLower() == dto.Nev.ToLower());
            if (letezik)
                return BadRequest("Ez a felszereltség már létezik!");

            var ujFelszereltseg = new Felszereltseg { Nev = dto.Nev };
            _context.Set<Felszereltseg>().Add(ujFelszereltseg);

            var adminId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "1");
            _context.AuditLogok.Add(new AuditLog
            {
                Muvelet = "Felszereltseg Felvétele",
                Reszletek = $"Új felszereltség rögzítve: {dto.Nev} nevű eszköz.",
                UzemeltetesiIgazgatoID = adminId,
                Idobelyeg = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            return Ok(ujFelszereltseg);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Adminisztrator")]
        public async Task<IActionResult> DeleteFelszereltseg(int id)
        {
            var felszereltseg = await _context.Set<Felszereltseg>()
                .Include(f => f.Termek)
                .FirstOrDefaultAsync(f => f.FelszereltsegID == id);

            if (felszereltseg == null) return NotFound();

            if (felszereltseg.Termek != null && felszereltseg.Termek.Any())
                return BadRequest("A felszereltség nem törölhető, mert már hozzá van rendelve egy vagy több teremhez!");

            string nev = felszereltseg.Nev;
            _context.Set<Felszereltseg>().Remove(felszereltseg);

            var adminId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "1");
            _context.AuditLogok.Add(new AuditLog
            {
                Muvelet = "Felszereltseg Törlése",
                Reszletek = $"Felszereltség törölve: {nev} nevű eszköz.",
                UzemeltetesiIgazgatoID = adminId,
                Idobelyeg = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            return NoContent();
        }
    }

    public class FelszereltsegInputDto
    {
        public string Nev { get; set; } = "";
    }
}