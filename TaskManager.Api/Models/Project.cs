using Microsoft.AspNetCore.Http.HttpResults;

namespace TaskManager.Api.Models
{
    public class Project
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        //1-N
        public List<TaskItem> Tasks { get; set; } = new();

        //N-N con User
        public List<User> Members { get; set; } = new();
    }
}
