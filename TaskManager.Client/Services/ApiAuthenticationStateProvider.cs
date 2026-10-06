using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;

namespace TaskManager.Client.Services
{
    // Blazor chiede sempre a QUESTA classe "chi è l'utente attuale?" (è lei che
    // decide cosa vedono <AuthorizeView>/[Authorize] in ogni pagina). Di base,
    // Blazor non saprebbe come rispondere: è compito nostro insegnarglielo,
    // leggendo il token JWT salvato nel browser al login.
    public class ApiAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly ILocalStorageService _localStorage;

        // Un utente "anonimo" di base: nessun claim, nessuna identità -> non autenticato.
        private static readonly ClaimsPrincipal Anonymous = new(new ClaimsIdentity());

        public ApiAuthenticationStateProvider(ILocalStorageService localStorage)
        {
            _localStorage = localStorage;
        }

        // Metodo che Blazor richiama ogni volta che deve sapere lo stato di autenticazione
        // (es. quando una pagina con [Authorize] viene caricata).
        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var token = await _localStorage.GetItemAsync<string>("authToken");

            if (string.IsNullOrWhiteSpace(token))
            {
                return new AuthenticationState(Anonymous);
            }

            // Il token è "header.payload.signature": a noi qui serve solo il payload,
            // dove sono scritti i claims (Id, Nome, Email, Ruolo) messi dal server
            // in TokenService.CreateToken(). Non verifichiamo la firma lato client:
            // non ne avremmo nemmeno la chiave segreta (giustamente). La verifica
            // "vera" la fa sempre e solo il server a ogni richiesta protetta.
            var claims = ParseClaimsFromJwt(token);
            var identity = new ClaimsIdentity(claims, authenticationType: "jwt");
            var user = new ClaimsPrincipal(identity);

            return new AuthenticationState(user);
        }

        // Da chiamare subito dopo login/logout, per forzare Blazor a rileggere
        // lo stato di autenticazione e ridisegnare la UI (NavMenu, pagine protette, ecc.).
        public void NotifyUserChanged()
        {
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }

        private static IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
        {
            // Il payload è la seconda delle tre parti separate da '.'
            var payload = jwt.Split('.')[1];
            var jsonBytes = ParseBase64WithoutPadding(payload);
            var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes)
                                 ?? new Dictionary<string, object>();

            return keyValuePairs.Select(kvp => new Claim(kvp.Key, kvp.Value.ToString() ?? string.Empty));
        }

        private static byte[] ParseBase64WithoutPadding(string base64)
        {
            // Il JWT usa "Base64Url", che non include il padding '=' che Base64
            // normale richiede: va ri-aggiunto a mano prima di poterlo decodificare.
            base64 = base64.Replace('-', '+').Replace('_', '/');
            switch (base64.Length % 4)
            {
                case 2: base64 += "=="; break;
                case 3: base64 += "="; break;
            }
            return Convert.FromBase64String(base64);
        }
    }
}
