using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManager.Api.Data;
using TaskManager.Api.Models;

namespace TaskManager.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TasksController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TasksController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<List<TaskItem>> GetAllAsync()
        {
            return await _context.TaskItems.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TaskItem>> GetById(int id)
        {
            var existingTask = await _context.TaskItems.FindAsync(id);

            return existingTask == null ? NotFound() : Ok(existingTask);
        }

        [HttpPost]
        public async Task<ActionResult<TaskItem>> AddAsync(TaskItem newTaskItem)
        {
            await _context.TaskItems.AddAsync(newTaskItem);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = newTaskItem.Id }, newTaskItem); ;
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateAsync(int id, TaskItem updatedTaskItem)
        {
            var existingTask= await _context.TaskItems.FindAsync(id);

            if (existingTask == null)
            {
                return NotFound();
            }
            existingTask.Title = updatedTaskItem.Title;
            existingTask.Description = updatedTaskItem.Description;
            existingTask.Status = updatedTaskItem.Status;
            existingTask.ReferenceProjectId = updatedTaskItem.ReferenceProjectId;
            existingTask.AssignedUserId = updatedTaskItem.AssignedUserId;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteAsync(int id)
        {
            var existingTask = await _context.TaskItems.FindAsync(id);

            if (existingTask == null)
            {
                return NotFound();
            }
            _context.Remove(existingTask);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }

}

