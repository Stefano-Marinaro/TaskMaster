using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Metadata.Ecma335;
using TaskManager.Api.Data;
using TaskManager.Api.Models;

namespace TaskManager.Api.Controllers
{
    [ApiController] //dic equesta è un API, gestisci automaticamente la validazione dei dati in ingresso e le risposte in formato JSON
    [Route("api/[controller]")] //definisce la route/URL del controller
    public class ProjectsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProjectsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<List<Project>> GetProjectsAsync()
        {
            var query = await _context.Projects.ToListAsync();
            return query;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Project>> GetById(int id)
        {
            var project = await _context.Projects.FindAsync(id);
            return project == null ? NotFound() : Ok(project);
        }

        [HttpPost]
        public async Task<ActionResult<Project>> AddProjectAsync(Project newProject)
        {
            await _context.Projects.AddAsync(newProject);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = newProject.Id}, newProject);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateAsync(int id, Project updatedProject)
        {
            var existingProject = await _context.Projects.FindAsync(id);

            if (existingProject == null)
            {
                return NotFound();
            }
            existingProject.Name = updatedProject.Name;
            existingProject.Description = updatedProject.Description;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteAsync(int id)
        {
            var existingProject = await _context.Projects.FindAsync(id);

            if (existingProject == null)
            {
                return NotFound();
            }
            _context.Remove(existingProject);
            await _context.SaveChangesAsync();

            return NoContent();
        }

    }
}