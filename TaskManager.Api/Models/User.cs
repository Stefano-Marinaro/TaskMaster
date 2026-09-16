namespace TaskManager.Api.Models
{
    public class User
    {
        public int Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public UserRole Role { get; set; } = UserRole.Member;

        // Relazione N-N: Piu utenti possono avere più progetti
        public List<Project> Projects { get; set; } = new();
        //Relazione 1-N: Piu di un task per questo utente
        public List<TaskItem> AssignedTasks { get; set; } = new();
    }

    public enum UserRole
    {
        Owner,
        Member
    }
}
