using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OrderManager.App.Data;
using OrderManager.App.DTOs;
using OrderManager.App.Models;

namespace OrderManager.App.Controllers
{
    [ApiController]
    [Route("api/auth")] // Теперь совпадает с JS (/api/auth/register и /api/auth/login)
    
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _db;

        public AuthController(AppDbContext db)
        {
            _db = db;
        }

        [HttpPost("register")]
        public async Task<ActionResult<AuthResponseDto>> Register(RegisterDto dto)
        {
            if (await _db.Users.AnyAsync(u => u.Email == dto.Email))
                return BadRequest("Пользователь с таким email уже существует");

            var user = new User
            {
                FullName = dto.FullName,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = "Client"
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            return Ok(new AuthResponseDto
            {
                Token = GenerateToken(user),
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role
            });
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login(LoginDto dto)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                return Unauthorized("Неверный email или пароль");

            if (user.IsBlocked)
                return Unauthorized("Ваша учётная запись заблокирована.");

            return Ok(new AuthResponseDto
            {
                Token = GenerateToken(user),
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role
            });
        }

        private string GenerateToken(User user)
        {
            var key = new SymmetricSecurityKey(
               Encoding.UTF8.GetBytes("YourSuperSecretKey32Bytes!123456"));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("fullName", user.FullName)
            };

            var token = new JwtSecurityToken(
                issuer: "OrderManager",
                audience: "OrderManager",
                claims: claims,
                expires: DateTime.UtcNow.AddDays(7),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}