using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManager.Api.Data;
using TaskManager.Api.Models;

namespace TaskManager.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CommentsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CommentsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<List<Comment>> GetAllAsync()
        {
            return await _context.Comments.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Comment>> GetById(int id)
        {
            var existing = await _context.Comments.FindAsync(id);
            return existing == null ? NotFound() : Ok(existing);
        }

        [HttpPost]
        public async Task<ActionResult<Comment>> AddAsync(Comment newComment)
        {
            await _context.Comments.AddAsync(newComment);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = newComment.Id }, newComment);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateAsync(int id, Comment updatedComment)
        {
            var existing = await _context.Comments.FindAsync(id);
            if (existing == null)
            {
                return NotFound();
            }

            existing.Content = updatedComment.Content;
            existing.TaskItemId = updatedComment.TaskItemId;
            existing.AuthorId = updatedComment.AuthorId;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteAsync(int id)
        {
            var existing = await _context.Comments.FindAsync(id);
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
