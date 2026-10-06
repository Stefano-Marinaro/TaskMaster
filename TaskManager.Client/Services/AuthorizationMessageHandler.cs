using System.Net.Http.Headers;
using Blazored.LocalStorage;

namespace TaskManager.Client.Services
{
    // Un DelegatingHandler si "aggancia" davanti a ogni richiesta fatta da un
    // HttpClient configurato per usarlo, e può modificarla prima che parta.
    // Qui lo usiamo per aggiungere automaticamente l'header
    //   Authorization: Bearer <token>
    // a ogni chiamata verso l'API, senza doverlo ripetere in ogni pagina Razor.
    public class AuthorizationMessageHandler : DelegatingHandler
    {
        private readonly ILocalStorageService _localStorage;

        public AuthorizationMessageHandler(ILocalStorageService localStorage)
        {
            _localStorage = localStorage;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var token = await _localStorage.GetItemAsync<string>("authToken", cancellationToken);

            if (!string.IsNullOrWhiteSpace(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            // Passa la richiesta (ora con l'header aggiunto) al prossimo anello della catena,
            // che per noi è semplicemente l'invio HTTP reale.
            return await base.SendAsync(request, cancellationToken);
        }
    }
}
