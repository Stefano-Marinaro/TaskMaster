using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManager.Api.Data;
using TaskManager.Api.Models;
using TaskManager.Api.Dtos;
using TaskManager.Api.Services;

namespace TaskManager.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly PasswordService _passwordService;
        private readonly TokenService _tokenService;

        public AuthController(AppDbContext context, PasswordService passwordService, TokenService tokenService)
        {
            _context = context;
            _passwordService = passwordService;
            _tokenService = tokenService;
        }

        [HttpPost("register")]
        public async Task<ActionResult<UserDto>> Register(RegisterDto dto)
        {
            var newUser = new User
            {
                UserName = dto.UserName,
                Email = dto.Email
            };

            newUser.PasswordHash = _passwordService.HashPassword(newUser, dto.Password );

            await _context.Users.AddAsync(newUser);
            await _context.SaveChangesAsync();

            var userDto = new UserDto
            {
                Id = newUser.Id,
                UserName = newUser.UserName,
                Email = newUser.Email,
                Role = newUser.Role
            };

            return Ok(userDto);
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login(LoginDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null)
            {
                return Unauthorized("Email o password non validi.");
            }

            var isPasswordValid = _passwordService.VerifyPassword(user, user.PasswordHash, dto.Password);
            if (!isPasswordValid)
            {
                return Unauthorized("Email o password non validi.");
            }

            var token = _tokenService.CreateToken(user);

            var response = new AuthResponseDto
            {
                Token = token,
                ExpiresAt = _tokenService.GetExpiration(),
                User = new UserDto
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    Role = user.Role
                }
            };

            return Ok(response);
        }
    }
}