using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;

//using WebApi.Data;

namespace WebApi.Controllers
{
    //[Route("api/login")]
    //[ApiController]
    //public class LoginController : ControllerBase
    //{
    //    private readonly AppDbContext _context;

    //    public LoginController(AppDbContext context)
    //    {
    //        _context = context;
    //    }

    //    [HttpGet] // <-- ÁTÍRTuk GET-RE, ÍGY NINCS ANTIFORGERY ELLENŐRZÉS SE STEM
    //    public async Task<IActionResult> Login([FromQuery] string username, [FromQuery] string password)
    //    {
    //        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
    //        {
    //            return BadRequest("A felhasználónév és a jelszó megadása kötelező.");
    //        }

    //        var user = await _context.Felhasznalok
    //            .FirstOrDefaultAsync(u => u.Email == username);

    //        if (user == null || user.Jelszo != password)
    //        {
    //            return Unauthorized("Hibás felhasználónév vagy jelszó.");
    //        }

    //        return Ok(new
    //        {
    //            Token = "dummy-jwt-token",
    //            Role = user.Szerepkor,
    //            Username = user.Nev
    //        });
    //    }
    //}
}