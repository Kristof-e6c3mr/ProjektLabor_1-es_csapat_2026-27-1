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
    public class MesterkulcsJogosultsagController : ControllerBase
    {
        private readonly AppDbContext _context;

        public MesterkulcsJogosultsagController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetJogosultsagok()
        {
            var lista = await _context.MesterkulcsJogosultsagok
                .Include(m => m.Felhasznalo)
                .Include(m => m.Kulcs)
                .Select(m => new
                {
                    m.JogosultsagID,
                    m.FelhasznaloID,
                    FelhasznaloNev = m.Felhasznalo != null ? m.Felhasznalo.Nev : "Ismeretlen",
                    Szerepkor = m.Felhasznalo != null ? m.Felhasznalo.Szerepkor : "-",
                    m.KulcsID,
                    m.ErvenyessegKezdete,
                    m.ErvenyessegVege
                })
                .OrderByDescending(m => m.ErvenyessegKezdete)
                .ToListAsync();

            return Ok(lista);
        }

        [HttpGet("felhasznalok")]
        public async Task<IActionResult> GetFelhasznalok()
        {
            var felhasznalok = await _context.Felhasznalok
                .Select(f => new { f.Id, f.Nev, f.Szerepkor })
                .OrderBy(f => f.Nev)
                .ToListAsync();

            return Ok(felhasznalok);
        }

        [HttpGet("mesterkulcsok")]
        public async Task<IActionResult> GetMesterkulcsok()
        {
            var kulcsok = await _context.Kulcsok
                .Where(k => k.MesterkulcsE)
                .Select(k => new { k.KulcsID })
                .ToListAsync();

            return Ok(kulcsok);
        }

        [HttpPost]
        public async Task<IActionResult> UjJogosultsag([FromBody] UjMesterkulcsJogosultsagDto dto)
        {
            if (dto.ErvenyessegKezdete >= dto.ErvenyessegVege)
                return BadRequest("Az érvényesség vége nem lehet korábban, mint a kezdete!");

            var mesterkulcs = await _context.Kulcsok.FindAsync(dto.KulcsID);
            if (mesterkulcs == null || !mesterkulcs.MesterkulcsE)
                return BadRequest("Érvénytelen mesterkulcs!");

            var utcKezdet = dto.ErvenyessegKezdete.ToUniversalTime();
            var utcVeg = dto.ErvenyessegVege.ToUniversalTime();

            bool vanMarJoga = await _context.MesterkulcsJogosultsagok.AnyAsync(m =>
                m.FelhasznaloID == dto.FelhasznaloID &&
                m.KulcsID == dto.KulcsID &&
                m.ErvenyessegKezdete < utcVeg &&
                m.ErvenyessegVege > utcKezdet);

            if (vanMarJoga)
                return BadRequest("Ennek a felhasználónak már van érvényes jogosultsága erre a mesterkulcsra a megadott időszakban!");

            var jogosultsag = new MesterkulcsJogosultsag
            {
                FelhasznaloID = dto.FelhasznaloID,
                KulcsID = dto.KulcsID,
                ErvenyessegKezdete = utcKezdet,
                ErvenyessegVege = utcVeg
            };

            _context.MesterkulcsJogosultsagok.Add(jogosultsag);
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Modositas(int id, [FromBody] UjMesterkulcsJogosultsagDto dto)
        {
            var jogosultsag = await _context.MesterkulcsJogosultsagok.FindAsync(id);
            if (jogosultsag == null) return NotFound();

            if (dto.ErvenyessegKezdete >= dto.ErvenyessegVege)
                return BadRequest("Az érvényesség vége nem lehet korábban, mint a kezdete!");

            var mesterkulcs = await _context.Kulcsok.FindAsync(dto.KulcsID);
            if (mesterkulcs == null || !mesterkulcs.MesterkulcsE)
                return BadRequest("Érvénytelen mesterkulcs!");

            var utcKezdet = dto.ErvenyessegKezdete.ToUniversalTime();
            var utcVeg = dto.ErvenyessegVege.ToUniversalTime();

            bool vanMarJoga = await _context.MesterkulcsJogosultsagok.AnyAsync(m =>
                m.JogosultsagID != id &&
                m.FelhasznaloID == dto.FelhasznaloID &&
                m.KulcsID == dto.KulcsID &&
                m.ErvenyessegKezdete < utcVeg &&
                m.ErvenyessegVege > utcKezdet);

            if (vanMarJoga)
                return BadRequest("Ennek a felhasználónak már van érvényes jogosultsága erre a mesterkulcsra a módosított időszakban!");

            jogosultsag.FelhasznaloID = dto.FelhasznaloID;
            jogosultsag.KulcsID = dto.KulcsID;
            jogosultsag.ErvenyessegKezdete = utcKezdet;
            jogosultsag.ErvenyessegVege = utcVeg;

            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Visszavonas(int id)
        {
            var jogosultsag = await _context.MesterkulcsJogosultsagok.FindAsync(id);
            if (jogosultsag == null) return NotFound();

            _context.MesterkulcsJogosultsagok.Remove(jogosultsag);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }

    public class UjMesterkulcsJogosultsagDto
    {
        public int FelhasznaloID { get; set; }
        public int KulcsID { get; set; }
        public DateTime ErvenyessegKezdete { get; set; }
        public DateTime ErvenyessegVege { get; set; }
    }
}