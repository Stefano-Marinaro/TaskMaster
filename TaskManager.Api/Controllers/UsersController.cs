using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using TaskManager.Api.Data;
using TaskManager.Api.Dtos;
using TaskManager.Api.Models;

namespace TaskManager.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UsersController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<List<UserDto>> GetAllAsync()
        {
            var users = await _context.Users.ToListAsync();
            var dto = users.Select(u => new UserDto
            {
                UserName = u.UserName,
                Id = u.Id,
                Email = u.Email,
                Role = u.Role
            }).ToList();
            return dto;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<UserDto>> GetById(int id)
        {
            var existing = await _context.Users.FindAsync(id);
            if (existing == null) return NotFound();
            var dto = new UserDto { Id = existing.Id, UserName = existing.UserName, Email = existing.Email, Role = existing.Role };
            return Ok(dto);
        }

        [HttpPost]
        public async Task<ActionResult<UserDto>> AddAsync(User newUser)
        {
            await _context.Users.AddAsync(newUser);
            await _context.SaveChangesAsync();
            var dto = new UserDto { Id = newUser.Id, UserName = newUser.UserName, Email = newUser.Email, Role = newUser.Role };
            return CreatedAtAction(nameof(GetById), new { id = newUser.Id }, dto);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateAsync(int id, UserDto updatedUser)
        {
            var existing = await _context.Users.FindAsync(id);
            if (existing == null)
            {
                return NotFound();
            }

            existing.UserName = updatedUser.UserName;
            existing.Email = updatedUser.Email;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteAsync(int id)
        {
            var existing = await _context.Users.FindAsync(id);
            if (existing == null)
            {
                return NotFound();
            }
            _context.Remove(existing);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
