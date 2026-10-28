using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using WebApi.Data;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class LoginController : ControllerBase
    {
        private readonly AppDbContext _context;

        public LoginController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest("A felhasználónév és a jelszó megadása kötelező.");
            }

            // Megkeressük a felhasználót email vagy név alapján
            var user = await _context.Felhasznalok
                .FirstOrDefaultAsync(u => u.Email == request.Username || u.Nev == request.Username);

            // BCrypt jelszó ellenőrzés a plain-text helyett
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.Jelszo))
            {
                return Unauthorized("Hibás felhasználónév vagy jelszó.");
            }

            var key = Encoding.ASCII.GetBytes("NagyonTitkosKulcsAmiLegalabb32KarakterHosszu!");
            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Nev),
                    new Claim(ClaimTypes.Role, user.Szerepkor),
                    new Claim("role", user.Szerepkor)
                }),
                Expires = DateTime.UtcNow.AddHours(8),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            return Ok(new
            {
                id = user.Id,
                username = user.Nev,
                role = user.Szerepkor,
                token = tokenString
            });
        }
    }

    public class LoginRequest
    {
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
    }
}