using System.ComponentModel.DataAnnotations;

namespace TaskManager.Client.Models
{
    // Stessa forma del RegisterDto lato server: il client deve mandare
    // esattamente i campi che il server si aspetta di trovare nel body JSON.
    // Le [DataAnnotations] qui sotto servono SOLO alla validazione lato
    // client (messaggi immediati nel form, senza nemmeno contattare il
    // server) — il server fa comunque le sue verifiche indipendentemente,
    // perché non ci si deve mai fidare solo dei controlli fatti nel browser.
    public class RegisterModel
    {
        [Required(ErrorMessage = "Il nome utente è obbligatorio.")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "L'email è obbligatoria.")]
        [EmailAddress(ErrorMessage = "Inserisci un'email valida.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "La password è obbligatoria.")]
        [MinLength(6, ErrorMessage = "La password deve avere almeno 6 caratteri.")]
        public string Password { get; set; } = string.Empty;
    }

    public class LoginModel
    {
        [Required(ErrorMessage = "L'email è obbligatoria.")]
        [EmailAddress(ErrorMessage = "Inserisci un'email valida.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "La password è obbligatoria.")]
        public string Password { get; set; } = string.Empty;
    }

    // Rispecchia AuthResponseDto lato server: quello che arriva dopo un login riuscito.
    public class AuthResponse
    {
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public UserInfo User { get; set; } = new();
    }

    public class UserInfo
    {
        public int Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }
}
