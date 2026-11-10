using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using WebApi.Data;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "UzemeltetesiIgazgato")]
    public class AuditLogController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AuditLogController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAuditLogok()
        {
            var logok = await _context.AuditLogok
                .Include(a => a.UzemeltetesiIgazgato)
                .Select(a => new
                {
                    NaploID = a.NaploID,
                    Idobelyeg = a.Idobelyeg,
                    Muvelet = a.Muvelet,
                    Reszletek = a.Reszletek,
                    FelelosNev = a.UzemeltetesiIgazgato != null ? a.UzemeltetesiIgazgato.Nev : "Rendszer"
                })
                .OrderByDescending(a => a.Idobelyeg)
                .ToListAsync();

            return Ok(logok);
        }
    }
}