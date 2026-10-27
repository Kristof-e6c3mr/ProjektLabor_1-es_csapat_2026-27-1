using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using WebApi.Data;
using WebApi.Models;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class KarbantartasiIdosavController : ControllerBase
    {
        private readonly AppDbContext _context;

        public KarbantartasiIdosavController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetKarbantartasok()
        {
            var lista = await _context.KarbantartasiIdosavok
                .Include(k => k.Terem)
                .Select(k => new
                {
                    k.KarbantartasID,
                    k.TeremID,
                    Teremszam = k.Terem != null ? k.Terem.Teremszam : "-",
                    k.KezdoIdo,
                    k.ZaroIdo,
                    k.Leiras
                })
                .OrderByDescending(k => k.KezdoIdo)
                .ToListAsync();

            return Ok(lista);
        }

        // --- MÓDOSÍTVA: Ütközések lekérése (Foglalások + Karbantartások) ---
        [HttpPost("utkozesek")]
        public async Task<IActionResult> GetUtkozesek([FromBody] KarbantartasKeresDto dto)
        {
            var utcKezdo = dto.KezdoIdo.ToUniversalTime();
            var utcZaro = dto.ZaroIdo.ToUniversalTime();

            var utkozoFoglalasok = await _context.Foglalasok
                .Include(f => f.Oktato)
                .Include(f => f.Terem)
                .Where(f => f.TeremID == dto.TeremID &&
                            f.Allapot != "Torolve" &&
                            f.KezdoIdo < utcZaro &&
                            f.ZaroIdo > utcKezdo)
                .Select(f => new
                {
                    f.FoglalasID,
                    Teremszam = f.Terem.Teremszam,
                    OktatoNev = f.Oktato.Nev,
                    f.KezdoIdo,
                    f.ZaroIdo
                })
                .ToListAsync();

            var utkozoKarbantartasok = await _context.KarbantartasiIdosavok
                .Include(k => k.Terem)
                .Where(k => k.TeremID == dto.TeremID &&
                            k.KarbantartasID != dto.KarbantartasID && // Saját magát ne mutassa ütközésként
                            k.KezdoIdo < utcZaro &&
                            k.ZaroIdo > utcKezdo)
                .Select(k => new
                {
                    k.KarbantartasID,
                    Teremszam = k.Terem.Teremszam,
                    k.KezdoIdo,
                    k.ZaroIdo,
                    k.Leiras
                })
                .ToListAsync();

            return Ok(new { Foglalasok = utkozoFoglalasok, Karbantartasok = utkozoKarbantartasok });
        }

        [HttpPost]
        public async Task<IActionResult> UjKarbantartas([FromBody] KarbantartasKeresDto dto)
        {
            var utcKezdo = dto.KezdoIdo.ToUniversalTime();
            var utcZaro = dto.ZaroIdo.ToUniversalTime();

            bool vanFoglalas = await _context.Foglalasok.AnyAsync(f =>
                f.TeremID == dto.TeremID && f.Allapot != "Torolve" &&
                f.KezdoIdo < utcZaro && f.ZaroIdo > utcKezdo);

            bool vanMasKarbantartas = await _context.KarbantartasiIdosavok.AnyAsync(k =>
                k.TeremID == dto.TeremID &&
                k.KezdoIdo < utcZaro && k.ZaroIdo > utcKezdo);

            if (vanFoglalas || vanMasKarbantartas)
                return BadRequest("A megadott idősávban ütköző foglalások vagy karbantartások vannak! Előbb ürítsd ki azokat.");

            var karbantartas = new KarbantartasiIdosav
            {
                TeremID = dto.TeremID,
                KezdoIdo = utcKezdo,
                ZaroIdo = utcZaro,
                Leiras = dto.Leiras ?? "Rendszeres karbantartás"
            };

            _context.KarbantartasiIdosavok.Add(karbantartas);
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Modositas(int id, [FromBody] KarbantartasKeresDto dto)
        {
            var karbantartas = await _context.KarbantartasiIdosavok.FindAsync(id);
            if (karbantartas == null) return NotFound();

            var utcKezdo = dto.KezdoIdo.ToUniversalTime();
            var utcZaro = dto.ZaroIdo.ToUniversalTime();

            bool vanFoglalas = await _context.Foglalasok.AnyAsync(f =>
                f.TeremID == dto.TeremID && f.Allapot != "Torolve" &&
                f.KezdoIdo < utcZaro && f.ZaroIdo > utcKezdo);

            bool vanMasKarbantartas = await _context.KarbantartasiIdosavok.AnyAsync(k =>
                k.KarbantartasID != id && k.TeremID == dto.TeremID &&
                k.KezdoIdo < utcZaro && k.ZaroIdo > utcKezdo);

            if (vanFoglalas || vanMasKarbantartas)
                return BadRequest("A módosított idősávban ütköző foglalások vagy karbantartások vannak!");

            karbantartas.TeremID = dto.TeremID;
            karbantartas.KezdoIdo = utcKezdo;
            karbantartas.ZaroIdo = utcZaro;
            karbantartas.Leiras = dto.Leiras ?? "Rendszeres karbantartás";

            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Torles(int id)
        {
            var karbantartas = await _context.KarbantartasiIdosavok.FindAsync(id);
            if (karbantartas == null) return NotFound();

            _context.KarbantartasiIdosavok.Remove(karbantartas);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }

    public class KarbantartasKeresDto
    {
        public int KarbantartasID { get; set; } // Szükséges az önmaga kizárásához szerkesztéskor
        public int TeremID { get; set; }
        public DateTime KezdoIdo { get; set; }
        public DateTime ZaroIdo { get; set; }
        public string? Leiras { get; set; }
    }
}