using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using WebApi.Data;
using WebApi.Models;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class KulcsMozgasController : ControllerBase
    {
        private readonly AppDbContext _context;

        public KulcsMozgasController(AppDbContext context)
        {
            _context = context;
        }

        // Oktatók lekérése a lenyílóhoz
        [HttpGet("oktatok")]
        public async Task<IActionResult> GetOktatok()
        {
            // Valós környezetben itt szűrhetnénk a Szerepkor == "Oktato" mezőre
            var oktatok = await _context.Felhasznalok
                .Select(f => new { f.Id, f.Nev })
                .ToListAsync();

            return Ok(oktatok);
        }

        [HttpPost("kiadas")]
        public async Task<IActionResult> KulcsKiadas([FromBody] KulcsKiadasDto dto)
        {
            var kulcs = await _context.Kulcsok.FindAsync(dto.KulcsID);
            if (kulcs == null || kulcs.KiadvaE)
                return BadRequest("A kulcs nem található vagy már ki van adva.");

            var mozgas = new KulcsMozgas
            {
                KulcsID = dto.KulcsID,
                AtvevoID = dto.OktatoID,
                PortasID = 1, // TODO: Bejelentkezett portás ID-ja a JWT tokenből
                Idobelyeg = DateTime.UtcNow
            };

            kulcs.KiadvaE = true;
            _context.KulcsMozgasok.Add(mozgas);
            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpPost("visszavetel/{kulcsId}")]
        public async Task<IActionResult> KulcsVisszavetel(int kulcsId)
        {
            var kulcs = await _context.Kulcsok.FindAsync(kulcsId);
            if (kulcs == null || !kulcs.KiadvaE)
                return BadRequest("A kulcs nem található vagy már a portán van.");

            // Visszavétel regisztrálása (opcionálisan ehhez is lehetne külön mozgásrekord)
            kulcs.KiadvaE = false;
            await _context.SaveChangesAsync();

            return Ok();
        }
    }

    public class KulcsKiadasDto
    {
        public int KulcsID { get; set; }
        public int OktatoID { get; set; }
    }
}