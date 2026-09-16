namespace TaskManager.Api.Models
{
    public class TaskItem //modello per rappresentare un'attività
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty; // evita che title sia null, inizializzandolo con una stringa vuota
        public string? Description { get; set; }
        public TaskState Status { get; set; } = TaskState.ToDo;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public enum TaskState//lo status dell'attività
    {
        ToDo,
        InProgress,
        Done
    }
}
