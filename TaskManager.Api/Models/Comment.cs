namespace TaskManager.Api.Models
{
    public class Comment
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Foreign key + Navigation Property verso TaskItem
        public int TaskItemId { get; set; }
        public TaskItem? TaskItem { get; set; }

        // Foreign key + Navigation Property verso User (autore)
        public int AuthorId { get; set; }
        public User? Author { get; set; }
    }
}