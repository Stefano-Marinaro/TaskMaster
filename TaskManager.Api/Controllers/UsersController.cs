using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManager.Api.Data;
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
        public async Task<List<User>> GetAllAsync()
        {
            return await _context.Users.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetById(int id)
        {
            var existing = await _context.Users.FindAsync(id);
            return existing == null ? NotFound() : Ok(existing);
        }

        [HttpPost]
        public async Task<ActionResult<User>> AddAsync(User newUser)
        {
            await _context.Users.AddAsync(newUser);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = newUser.Id }, newUser);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateAsync(int id, User updatedUser)
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
