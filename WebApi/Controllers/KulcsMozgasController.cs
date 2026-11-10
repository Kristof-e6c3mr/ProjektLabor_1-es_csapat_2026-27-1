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
    public class KulcsMozgasController : ControllerBase
    {
        private readonly AppDbContext _context;

        public KulcsMozgasController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("oktatok")]
        [Authorize(Roles = "Portas")]
        public async Task<IActionResult> GetOktatok()
        {
            var oktatok = await _context.Felhasznalok
                .Select(f => new { f.Id, f.Nev })
                .ToListAsync();

            return Ok(oktatok);
        }

        [HttpPost("azonositas")]
        [Authorize(Roles = "Portas")]
        public async Task<IActionResult> AzonositasPinAlapjan([FromBody] PinKeresDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.PinKod))
                return BadRequest("A PIN kód megadása kötelező!");

            var oktato = await _context.Felhasznalok
                .FirstOrDefaultAsync(f => f.PinKod == dto.PinKod);

            if (oktato == null)
                return NotFound("Érvénytelen PIN kód!");

            return Ok(new { oktato.Id, oktato.Nev });
        }

        [HttpGet("tartozas/{oktatoId}")]
        [Authorize(Roles = "Portas")]
        public async Task<IActionResult> GetTartozas(int oktatoId)
        {
            var aktivMozgasok = await _context.KulcsMozgasok
                .Include(km => km.Kulcs)
                .ThenInclude(k => k.Terem)
                .Where(km => km.Kulcs != null && km.Kulcs.KiadvaE)
                .ToListAsync();

            var tartozasok = aktivMozgasok
                .GroupBy(km => km.KulcsID)
                .Select(g => g.OrderByDescending(x => x.Idobelyeg).First())
                .Where(km => km.AtvevoID == oktatoId)
                .Select(km => new TartozasDto
                {
                    KulcsID = km.KulcsID,
                    Megnevezes = km.Kulcs.MesterkulcsE ? "Mesterkulcs" : $"Terem: {km.Kulcs.Terem?.Teremszam}",
                    Kiadva = km.Idobelyeg
                })
                .ToList();

            return Ok(tartozasok);
        }

        [HttpGet("kiadhato-kulcsok/{oktatoId}")]
        [Authorize(Roles = "Portas")]
        public async Task<IActionResult> GetKiadhatoKulcsok(int oktatoId)
        {
            var maUtcKezdet = DateTime.UtcNow.Date;
            var maUtcVeg = maUtcKezdet.AddDays(1);
            var mostUtc = DateTime.UtcNow;

            var elerhetoKulcsok = new List<KiadhatoKulcsDto>();

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
                        Megnevezes = "Mesterkulcs",
                        Tipus = "Mesterkulcs",
                        Idopont = "Egész napos"
                    });
                }
            }

            return Ok(elerhetoKulcsok);
        }

        [HttpPost("kiadas")]
        [Authorize(Roles = "Portas")]
        public async Task<IActionResult> KulcsKiadas([FromBody] KulcsKiadasKeresDto dto)
        {
            var kulcs = await _context.Kulcsok
                .Include(k => k.Terem)
                .FirstOrDefaultAsync(k => k.KulcsID == dto.KulcsID);

            if (kulcs == null || kulcs.KiadvaE)
                return BadRequest("A kulcs nem található vagy már ki van adva.");

            var oktato = await _context.Felhasznalok.FindAsync(dto.OktatoID);
            var portas = await _context.Felhasznalok.FindAsync(dto.PortasID);

            string kulcsMegnevezes = kulcs.MesterkulcsE ? "Mesterkulcs" : $"{kulcs.Terem?.Teremszam} terem kulcsa";
            string oktatoNev = oktato?.Nev ?? "Ismeretlen oktató";
            string portasNev = portas?.Nev ?? "Ismeretlen portás";

            var ujAuditLog = new AuditLog
            {
                Muvelet = "Kulcs Kiadása",
                Reszletek = $"{kulcsMegnevezes} kiadva az alábbi oktatónak: {oktatoNev} ({portasNev} portás által).",
                UzemeltetesiIgazgatoID = 1,
                Idobelyeg = DateTime.UtcNow
            };

            var mozgas = new KulcsMozgas
            {
                KulcsID = dto.KulcsID,
                AtvevoID = dto.OktatoID,
                PortasID = dto.PortasID,
                AuditLog = ujAuditLog,
                Idobelyeg = DateTime.UtcNow
            };

            kulcs.KiadvaE = true;
            _context.KulcsMozgasok.Add(mozgas);
            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpPost("visszavetel/{kulcsId}")]
        [Authorize(Roles = "Portas")]
        public async Task<IActionResult> KulcsVisszavetel(int kulcsId)
        {
            var kulcs = await _context.Kulcsok
                .Include(k => k.Terem)
                .FirstOrDefaultAsync(k => k.KulcsID == kulcsId);

            if (kulcs == null || !kulcs.KiadvaE)
                return BadRequest("A kulcs nem található vagy már a portán van.");

            kulcs.KiadvaE = false;

            string kulcsMegnevezes = kulcs.MesterkulcsE ? "Mesterkulcs" : $"{kulcs.Terem?.Teremszam} terem kulcsának";

            var portasId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "1");
            var portas = await _context.Felhasznalok.FindAsync(portasId);
            string portasNev = portas?.Nev ?? "Portás";

            _context.AuditLogok.Add(new AuditLog
            {
                Muvelet = "Kulcs Visszavétele",
                Reszletek = $"{kulcsMegnevezes} visszavétele megtörtént a portára ({portasNev} által).",
                UzemeltetesiIgazgatoID = 1,
                Idobelyeg = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpGet("virtualis-szekreny")]
        [Authorize(Roles = "Portas,Adminisztrator")]
        public async Task<IActionResult> GetVirtualisSzekreny()
        {
            var mostUtc = DateTime.UtcNow;
            var maKezdet = mostUtc.Date;
            var maVeg = maKezdet.AddDays(1);

            var kulcsok = await _context.Kulcsok
                .Include(k => k.Terem)
                .ToListAsync();

            var aktivMozgasok = await _context.KulcsMozgasok
                .Include(km => km.Atvevo)
                .Where(km => km.Kulcs != null && km.Kulcs.KiadvaE)
                .ToListAsync();

            var maiFoglalasok = await _context.Foglalasok
                .Where(f => f.KezdoIdo >= maKezdet && f.KezdoIdo < maVeg && f.Allapot != "Torolve")
                .ToListAsync();

            var szekrenyLista = new List<VirtualisKulcsDto>();

            foreach (var k in kulcsok)
            {
                var aktivMozgas = aktivMozgasok.FirstOrDefault(km => km.KulcsID == k.KulcsID);
                string statusz = "Bent";
                string atvevoNev = "-";

                if (k.KiadvaE && aktivMozgas != null)
                {
                    statusz = "Kiadva";
                    atvevoNev = aktivMozgas.Atvevo?.Nev ?? "Ismeretlen";

                    var lejartFoglalas = maiFoglalasok.FirstOrDefault(f => f.TeremID == k.TeremID && f.OktatoID == aktivMozgas.AtvevoID && f.ZaroIdo < mostUtc);
                    if (lejartFoglalas != null || aktivMozgas.Idobelyeg.AddHours(4) < mostUtc)
                    {
                        statusz = "Késik";
                    }
                }
                else if (!k.KiadvaE && !k.MesterkulcsE)
                {
                    var teremFoglalasok = maiFoglalasok.Where(f => f.TeremID == k.TeremID).OrderBy(f => f.KezdoIdo).ToList();

                    var keso = teremFoglalasok.FirstOrDefault(f => f.KezdoIdo <= mostUtc && f.ZaroIdo > mostUtc);
                    if (keso != null)
                    {
                        statusz = "Késő felvétel";
                    }
                    else
                    {
                        var kozeli = teremFoglalasok.FirstOrDefault(f => f.KezdoIdo > mostUtc && f.KezdoIdo <= mostUtc.AddMinutes(30));
                        if (kozeli != null)
                        {
                            statusz = "Közeli foglalás";
                        }
                    }
                }

                string teremszam = k.Terem?.Teremszam ?? "";
                szekrenyLista.Add(new VirtualisKulcsDto
                {
                    KulcsID = k.KulcsID,
                    Megnevezes = k.MesterkulcsE ? "Mesterkulcs" : teremszam,
                    Statusz = statusz,
                    AtvevoNev = atvevoNev,
                    Teremszam = teremszam,
                    MesterkulcsE = k.MesterkulcsE
                });
            }

            return Ok(szekrenyLista);
        }
    }

    public class PinKeresDto
    {
        public string PinKod { get; set; } = "";
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

    public class TartozasDto
    {
        public int KulcsID { get; set; }
        public string Megnevezes { get; set; } = "";
        public DateTime Kiadva { get; set; }
    }

    public class VirtualisKulcsDto
    {
        public int KulcsID { get; set; }
        public string Megnevezes { get; set; } = "";
        public string Statusz { get; set; } = "";
        public string AtvevoNev { get; set; } = "";
        public string Teremszam { get; set; } = "";
        public bool MesterkulcsE { get; set; }
    }
}