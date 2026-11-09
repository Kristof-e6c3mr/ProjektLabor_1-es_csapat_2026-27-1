using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using WebApi.Data;
using WebApi.Models;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Bárki bejelentkezett lekérdezheti a listát
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
                .OrderBy(f => f.Nev) // ABC sorrendbe is rakjuk
                .ToListAsync();

            return Ok(lista);
        }

        [HttpPost]
        [Authorize(Roles = "Adminisztrator")] // Csak admin vehet fel újat
        public async Task<IActionResult> UjFelszereltseg([FromBody] FelszereltsegInputDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Nev))
                return BadRequest("A felszereltség neve nem lehet üres!");

            bool letezik = await _context.Set<Felszereltseg>().AnyAsync(f => f.Nev.ToLower() == dto.Nev.ToLower());
            if (letezik)
                return BadRequest("Ez a felszereltség már létezik!");

            var ujFelszereltseg = new Felszereltseg { Nev = dto.Nev };
            _context.Set<Felszereltseg>().Add(ujFelszereltseg);
            await _context.SaveChangesAsync();

            return Ok(ujFelszereltseg);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Adminisztrator")] // Csak admin törölhet
        public async Task<IActionResult> DeleteFelszereltseg(int id)
        {
            var felszereltseg = await _context.Set<Felszereltseg>()
                .Include(f => f.Termek)
                .FirstOrDefaultAsync(f => f.FelszereltsegID == id);

            if (felszereltseg == null) return NotFound();

            // Nem engedjük törölni, ha már van olyan terem, amiben ez benne van
            if (felszereltseg.Termek != null && felszereltseg.Termek.Any())
                return BadRequest("A felszereltség nem törölhető, mert már hozzá van rendelve egy vagy több teremhez!");

            _context.Set<Felszereltseg>().Remove(felszereltseg);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }

    public class FelszereltsegInputDto
    {
        public string Nev { get; set; } = "";
    }
}