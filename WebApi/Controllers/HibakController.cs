using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using WebApi.Data;
using WebApi.Models;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Portas")]
    public class HibakController : ControllerBase
    {
        private readonly AppDbContext _context;

        public HibakController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("termek")]
        public async Task<IActionResult> GetTermek()
        {
            var termek = await _context.Termek
                .Select(t => new { t.TeremID, t.Teremszam })
                .ToListAsync();

            return Ok(termek);
        }

        [HttpPost]
        public async Task<IActionResult> CreateHiba([FromBody] HibaCreateDto dto)
        {
            var hiba = new Hiba
            {
                Leiras = dto.Leiras,
                TeremID = dto.TeremID,
                PortasID = dto.PortasID,
                LetrehozasDatuma = DateTime.UtcNow,
                Allapot = false
            };

            _context.Hibak.Add(hiba);
            await _context.SaveChangesAsync();

            return Ok(hiba);
        }
    }

    public class HibaCreateDto
    {
        public string Leiras { get; set; } = string.Empty;
        public int TeremID { get; set; }
        public int PortasID { get; set; }
    }
}