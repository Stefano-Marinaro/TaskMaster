using TaskManager.Api.Models;

namespace TaskManager.Api.Dtos
{
    public class UserDto
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public UserRole Role { get; set; }
    }
}
