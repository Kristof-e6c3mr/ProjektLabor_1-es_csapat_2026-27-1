using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using WebApi.Data;
using WebApi.Models;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Bárki, aki be van jelentkezve, olvashatja a termeket
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
            var termek = await _context.Termek.ToListAsync();
            return Ok(termek);
        }

        [HttpPost]
        [Authorize(Roles = "Adminisztrator")] // Csak admin rögzíthet
        public async Task<IActionResult> CreateTerem([FromBody] Terem terem)
        {
            if (terem.Ferohely < 0)
            {
                return BadRequest("A férőhely nem lehet negatív!");
            }

            bool letezik = await _context.Termek.AnyAsync(t => t.Teremszam == terem.Teremszam);
            if (letezik)
            {
                return BadRequest("Ez a teremszám már létezik!");
            }

            _context.Termek.Add(terem);
            await _context.SaveChangesAsync();
            return Ok(terem);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Adminisztrator")] // Csak admin törölhet
        public async Task<IActionResult> DeleteTerem(int id)
        {
            var terem = await _context.Termek.FindAsync(id);
            if (terem == null) return NotFound();

            _context.Termek.Remove(terem);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}