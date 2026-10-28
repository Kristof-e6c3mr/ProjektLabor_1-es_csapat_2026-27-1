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
            var kulcs = new Kulcs
            {
                MesterkulcsE = dto.TeremID == 0,
                KiadvaE = false,
                TeremID = dto.TeremID == 0 ? null : dto.TeremID
            };

            _context.Kulcsok.Add(kulcs);
            await _context.SaveChangesAsync();

            return Ok(kulcs);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteKulcs(int id)
        {
            var kulcs = await _context.Kulcsok.FindAsync(id);
            if (kulcs == null) return NotFound();

            if (kulcs.KiadvaE)
            {
                return BadRequest("A kulcs jelenleg ki van adva, ezért nem törölhető!");
            }

            _context.Kulcsok.Remove(kulcs);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }

    public class UjKulcsDto
    {
        public int TeremID { get; set; }
    }
}