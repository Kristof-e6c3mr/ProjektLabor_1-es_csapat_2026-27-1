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
    public class FelszereltsegController : ControllerBase
    {
        private readonly AppDbContext _context;

        public FelszereltsegController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetFelszereltsegek()
        {
            var lista = await _context.Set<Felszereltseg>()
                .Select(f => new { f.FelszereltsegID, f.Nev })
                .ToListAsync();

            return Ok(lista);
        }
    }
}