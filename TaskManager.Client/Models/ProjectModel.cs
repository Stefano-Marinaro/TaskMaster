namespace TaskManager.Client.Models
{
    // Rispecchia il modello Project lato server (le liste Tasks/Members
    // non servono qui: la pagina Projects mostra solo i dati principali).
    public class ProjectModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
