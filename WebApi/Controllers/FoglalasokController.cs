using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using WebApi.Data;
using WebApi.Models;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Oktato,Adminisztrator")]
    public class FoglalasokController : ControllerBase
    {
        private readonly AppDbContext _context;

        public FoglalasokController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetFoglalasok()
        {
            var foglalasok = await _context.Foglalasok
                .Include(f => f.Terem)
                .Include(f => f.Oktato)
                .Select(f => new
                {
                    f.FoglalasID,
                    Teremszam = f.Terem != null ? f.Terem.Teremszam : "-",
                    OktatoNev = f.Oktato != null ? f.Oktato.Nev : "-",
                    f.KezdoIdo,
                    f.ZaroIdo,
                    f.Allapot
                })
                .OrderBy(f => f.KezdoIdo)
                .ToListAsync();

            return Ok(foglalasok);
        }

        [HttpGet("sajat/{oktatoId}")]
        public async Task<IActionResult> GetSajatFoglalasok(int oktatoId)
        {
            var foglalasok = await _context.Foglalasok
                .Include(f => f.Terem)
                .Include(f => f.Oktato)
                .Where(f => f.OktatoID == oktatoId)
                .Select(f => new
                {
                    f.FoglalasID,
                    Teremszam = f.Terem != null ? f.Terem.Teremszam : "-",
                    OktatoNev = f.Oktato != null ? f.Oktato.Nev : "-",
                    f.KezdoIdo,
                    f.ZaroIdo,
                    f.Allapot
                })
                .OrderByDescending(f => f.KezdoIdo)
                .ToListAsync();

            return Ok(foglalasok);
        }

        [HttpPost]
        public async Task<IActionResult> UjFoglalas([FromBody] UjFoglalasDto dto)
        {
            if (dto.KezdoIdo >= dto.ZaroIdo)
                return BadRequest("A foglalás vége nem lehet korábban, mint a kezdete!");

            var utcKezdo = dto.KezdoIdo.ToUniversalTime();
            var utcZaro = dto.ZaroIdo.ToUniversalTime();

            bool utkozik = await _context.Foglalasok.AnyAsync(f =>
                f.TeremID == dto.TeremID &&
                f.Allapot == "Aktiv" &&
                f.KezdoIdo < utcZaro &&
                f.ZaroIdo > utcKezdo);

            bool karbantartasUtkozik = await _context.KarbantartasiIdosavok.AnyAsync(k =>
                k.TeremID == dto.TeremID &&
                k.KezdoIdo < utcZaro &&
                k.ZaroIdo > utcKezdo);

            if (utkozik) return BadRequest("Ütközés! A megadott időpontban a terem már foglalt.");
            if (karbantartasUtkozik) return BadRequest("A megadott időpontban a terem karbantartás alatt áll!");

            var terem = await _context.Termek.FindAsync(dto.TeremID);
            var oktato = await _context.Felhasznalok.FindAsync(dto.OktatoID);

            string teremszam = terem?.Teremszam ?? "Ismeretlen";
            string oktatoNev = oktato?.Nev ?? "Ismeretlen oktató";

            var foglalas = new Foglalas
            {
                TeremID = dto.TeremID,
                OktatoID = dto.OktatoID,
                KezdoIdo = utcKezdo,
                ZaroIdo = utcZaro,
                Allapot = "Aktiv"
            };

            _context.Foglalasok.Add(foglalas);

            _context.AuditLogok.Add(new AuditLog
            {
                Muvelet = "Terem Foglalás",
                Reszletek = $"Új foglalás rögzítve: {oktatoNev} lefoglalta a(z) {teremszam} termet {utcKezdo:yyyy.MM.dd HH:mm} és {utcZaro:HH:mm} között.",
                UzemeltetesiIgazgatoID = dto.OktatoID,
                Idobelyeg = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> FoglalasModositas(int id, [FromBody] UjFoglalasDto dto)
        {
            var foglalas = await _context.Foglalasok
                .Include(f => f.Terem)
                .Include(f => f.Oktato)
                .FirstOrDefaultAsync(f => f.FoglalasID == id);

            if (foglalas == null) return NotFound();

            var utcKezdo = dto.KezdoIdo.ToUniversalTime();
            var utcZaro = dto.ZaroIdo.ToUniversalTime();

            bool utkozik = await _context.Foglalasok.AnyAsync(f =>
                f.FoglalasID != id &&
                f.TeremID == dto.TeremID &&
                f.Allapot == "Aktiv" &&
                f.KezdoIdo < utcZaro &&
                f.ZaroIdo > utcKezdo);

            bool karbUtkozik = await _context.KarbantartasiIdosavok.AnyAsync(k =>
                k.TeremID == dto.TeremID &&
                k.KezdoIdo < utcZaro &&
                k.ZaroIdo > utcKezdo);

            if (utkozik || karbUtkozik)
                return BadRequest("Az új időpont/terem ütközik egy másik foglalással vagy karbantartással!");

            var ujTerem = await _context.Termek.FindAsync(dto.TeremID);
            string teremszam = ujTerem?.Teremszam ?? foglalas.Terem?.Teremszam ?? "Ismeretlen";

            foglalas.TeremID = dto.TeremID;
            foglalas.KezdoIdo = utcKezdo;
            foglalas.ZaroIdo = utcZaro;

            _context.AuditLogok.Add(new AuditLog
            {
                Muvelet = "Foglalás Módosítása",
                Reszletek = $"Foglalás módosítva (ID: {id}): átkerült a(z) {teremszam} teremre ({utcKezdo:yyyy.MM.dd HH:mm} - {utcZaro:HH:mm}).",
                UzemeltetesiIgazgatoID = dto.OktatoID > 0 ? dto.OktatoID : 1,
                Idobelyeg = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Torles(int id)
        {
            var foglalas = await _context.Foglalasok
                .Include(f => f.Terem)
                .Include(f => f.Oktato)
                .FirstOrDefaultAsync(f => f.FoglalasID == id);

            if (foglalas == null) return NotFound();

            string teremszam = foglalas.Terem?.Teremszam ?? "Ismeretlen";
            string oktatoNev = foglalas.Oktato?.Nev ?? "Ismeretlen";
            string idoszak = $"{foglalas.KezdoIdo:yyyy.MM.dd HH:mm}-{foglalas.ZaroIdo:HH:mm}";

            _context.Foglalasok.Remove(foglalas);

            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "1");
            _context.AuditLogok.Add(new AuditLog
            {
                Muvelet = "Foglalás Törlése",
                Reszletek = $"Foglalás törölve (ID: {id}): {oktatoNev} által a(z) {teremszam} teremre rögzített ({idoszak}) idősávú foglalás.",
                UzemeltetesiIgazgatoID = userId,
                Idobelyeg = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            return NoContent();
        }
    }

    public class UjFoglalasDto
    {
        public int TeremID { get; set; }
        public int OktatoID { get; set; }
        public DateTime KezdoIdo { get; set; }
        public DateTime ZaroIdo { get; set; }
    }
}