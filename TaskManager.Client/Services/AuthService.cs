using System.Net.Http.Json;
using Blazored.LocalStorage;
using TaskManager.Client.Models;

namespace TaskManager.Client.Services
{
    // Centralizza tutto ciò che riguarda login/registrazione/logout, così le
    // pagine Razor (Login.razor, Register.razor) restano semplici: si limitano
    // a chiamare questi metodi e a mostrare il risultato, senza occuparsi di
    // HttpClient o di localStorage direttamente.
    public class AuthService
    {
        private readonly HttpClient _http;
        private readonly ILocalStorageService _localStorage;
        private readonly ApiAuthenticationStateProvider _authStateProvider;

        public AuthService(HttpClient http, ILocalStorageService localStorage, ApiAuthenticationStateProvider authStateProvider)
        {
            _http = http;
            _localStorage = localStorage;
            _authStateProvider = authStateProvider;
        }

        // Restituisce null in caso di successo, oppure un messaggio d'errore da mostrare all'utente.
        //
        // IMPORTANTE: una chiamata HTTP può fallire per due famiglie di motivi
        // completamente diverse, e vanno gestite entrambe:
        //  1. Il server ha risposto, ma con un errore (es. email già registrata)
        //     -> response.IsSuccessStatusCode è false, nessuna eccezione.
        //  2. La richiesta non è MAI arrivata a destinazione (API non avviata,
        //     blocco CORS, certificato HTTPS di sviluppo non attendibile,
        //     nessuna connessione) -> PostAsJsonAsync lancia un'eccezione
        //     (tipicamente HttpRequestException). Senza un try/catch qui,
        //     questa eccezione risale fino a Blazor e produce il banner generico
        //     "An unhandled error has occurred" in fondo alla pagina, lasciando
        //     la UI bloccata (es. il bottone fermo su "in corso...").
        public async Task<string?> RegisterAsync(RegisterModel model)
        {
            try
            {
                var response = await _http.PostAsJsonAsync("api/auth/register", model);

                if (response.IsSuccessStatusCode)
                {
                    return null;
                }

                return $"Registrazione non riuscita ({(int)response.StatusCode}). Controlla i dati inseriti.";
            }
            catch (Exception ex)
            {
                return DescribeConnectionError(ex);
            }
        }

        public async Task<string?> LoginAsync(LoginModel model)
        {
            try
            {
                var response = await _http.PostAsJsonAsync("api/auth/login", model);

                if (!response.IsSuccessStatusCode)
                {
                    return "Email o password non validi.";
                }

                var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
                if (authResponse is null)
                {
                    return "Risposta del server non valida.";
                }

                // Salviamo il token nel localStorage del browser: sopravvive alla
                // chiusura della pagina (a differenza di una semplice variabile in
                // memoria), così l'utente resta loggato anche ricaricando la pagina.
                await _localStorage.SetItemAsync("authToken", authResponse.Token);

                // Avvisiamo Blazor che lo stato di autenticazione è cambiato, in modo
                // che NavMenu e le pagine protette si aggiornino subito.
                _authStateProvider.NotifyUserChanged();

                return null;
            }
            catch (Exception ex)
            {
                return DescribeConnectionError(ex);
            }
        }

        // Messaggio comprensibile per l'utente, invece di far trapelare lo stack trace
        // tecnico dell'eccezione originale (che comunque resta visibile nella console
        // del browser, F12, per chi sta sviluppando).
        private static string DescribeConnectionError(Exception ex)
            => $"Impossibile contattare il server ({ex.Message}). " +
               "Verifica che TaskManager.Api sia in esecuzione e raggiungibile all'indirizzo configurato in wwwroot/appsettings.json.";

        public async Task LogoutAsync()
        {
            await _localStorage.RemoveItemAsync("authToken");
            _authStateProvider.NotifyUserChanged();
        }
    }
}
