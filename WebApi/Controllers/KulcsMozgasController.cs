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

        // 1. MEGLÉVŐ: Oktatók lekérése a lenyílóhoz (Megtartva)
        [HttpGet("oktatok")]
        public async Task<IActionResult> GetOktatok()
        {
            var oktatok = await _context.Felhasznalok
                .Select(f => new { f.Id, f.Nev })
                .ToListAsync();

            return Ok(oktatok);
        }

        // 2. ÚJ: Oktató azonosítása PIN kód alapján
        [HttpPost("azonositas")]
        public async Task<IActionResult> AzonositasPinAlapjan([FromBody] string pinKod)
        {
            var oktato = await _context.Felhasznalok
                .FirstOrDefaultAsync(f => f.PinKod == pinKod);

            if (oktato == null)
                return NotFound("Érvénytelen PIN kód!");

            return Ok(new { oktato.Id, oktato.Nev });
        }

        // 3. ÚJ: Az oktató kiadható kulcsainak lekérdezése
        [HttpGet("kiadhato-kulcsok/{oktatoId}")]
        public async Task<IActionResult> GetKiadhatoKulcsok(int oktatoId)
        {
            var maUtcKezdet = DateTime.UtcNow.Date;
            var maUtcVeg = maUtcKezdet.AddDays(1);
            var mostUtc = DateTime.UtcNow;

            var elerhetoKulcsok = new List<KiadhatoKulcsDto>();

            // A) Napi foglalásokhoz tartozó termek
            var maiFoglalasok = await _context.Foglalasok
                .Include(f => f.Terem)
                .Where(f => f.OktatoID == oktatoId &&
                            f.Allapot == "Aktiv" &&
                            f.KezdoIdo >= maUtcKezdet &&
                            f.KezdoIdo < maUtcVeg)
                .OrderBy(f => f.KezdoIdo)
                .ToListAsync();

            foreach (var foglalas in maiFoglalasok)
            {
                var teremKulcs = await _context.Kulcsok
                    .FirstOrDefaultAsync(k => k.TeremID == foglalas.TeremID && !k.KiadvaE && !k.MesterkulcsE);

                if (teremKulcs != null)
                {
                    elerhetoKulcsok.Add(new KiadhatoKulcsDto
                    {
                        KulcsID = teremKulcs.KulcsID,
                        Megnevezes = $"Terem: {foglalas.Terem?.Teremszam}",
                        Tipus = "Terem",
                        Idopont = $"{foglalas.KezdoIdo:HH:mm} - {foglalas.ZaroIdo:HH:mm}"
                    });
                }
            }

            // B) Érvényes mesterkulcs jogosultságok
            var mesterkulcsJogosultsagok = await _context.MesterkulcsJogosultsagok
                .Include(m => m.Kulcs)
                .Where(m => m.FelhasznaloID == oktatoId &&
                            m.ErvenyessegKezdete <= mostUtc &&
                            m.ErvenyessegVege >= mostUtc)
                .ToListAsync();

            foreach (var jog in mesterkulcsJogosultsagok)
            {
                if (jog.Kulcs != null && !jog.Kulcs.KiadvaE)
                {
                    elerhetoKulcsok.Add(new KiadhatoKulcsDto
                    {
                        KulcsID = jog.Kulcs.KulcsID,
                        Megnevezes = $"Mesterkulcs (ID: {jog.Kulcs.KulcsID})",
                        Tipus = "Mesterkulcs",
                        Idopont = "Egész napos"
                    });
                }
            }

            return Ok(elerhetoKulcsok);
        }

        // 4. FRISSÍTETT: Kulcskiadás bejelentkezett portással
        [HttpPost("kiadas")]
        public async Task<IActionResult> KulcsKiadas([FromBody] KulcsKiadasKeresDto dto)
        {
            var kulcs = await _context.Kulcsok.FindAsync(dto.KulcsID);
            if (kulcs == null || kulcs.KiadvaE)
                return BadRequest("A kulcs nem található vagy már ki van adva.");

            var mozgas = new KulcsMozgas
            {
                KulcsID = dto.KulcsID,
                AtvevoID = dto.OktatoID,
                PortasID = dto.PortasID, // A frontendről érkezik a JWT alapján
                Idobelyeg = DateTime.UtcNow
            };

            kulcs.KiadvaE = true;
            _context.KulcsMozgasok.Add(mozgas);
            await _context.SaveChangesAsync();

            return Ok();
        }

        // 5. MEGLÉVŐ: Kulcs visszavétele (Megtartva)
        [HttpPost("visszavetel/{kulcsId}")]
        public async Task<IActionResult> KulcsVisszavetel(int kulcsId)
        {
            var kulcs = await _context.Kulcsok.FindAsync(kulcsId);
            if (kulcs == null || !kulcs.KiadvaE)
                return BadRequest("A kulcs nem található vagy már a portán van.");

            kulcs.KiadvaE = false;
            await _context.SaveChangesAsync();

            return Ok();
        }
    }

    public class KiadhatoKulcsDto
    {
        public int KulcsID { get; set; }
        public string Megnevezes { get; set; } = "";
        public string Tipus { get; set; } = "";
        public string Idopont { get; set; } = "";
    }

    public class KulcsKiadasKeresDto
    {
        public int KulcsID { get; set; }
        public int OktatoID { get; set; }
        public int PortasID { get; set; }
    }
}