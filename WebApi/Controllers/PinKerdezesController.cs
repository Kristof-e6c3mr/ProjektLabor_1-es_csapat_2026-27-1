using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using WebApi.Data;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PinKerdezesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PinKerdezesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("sajat-pin/{oktatoId}")]
        [Authorize(Roles = "Portas,Oktato,Adminisztrator")]
        public async Task<IActionResult> GetSajatPin(int oktatoId)
        {
            var oktato = await _context.Felhasznalok.FindAsync(oktatoId);
            if (oktato == null || string.IsNullOrEmpty(oktato.PinKod))
                return NotFound("A felhasználó vagy a PIN kód nem található.");

            return Ok(new { pinKod = oktato.PinKod });
        }
    }
}