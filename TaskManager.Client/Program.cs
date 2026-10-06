using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using TaskManager.Client;
using TaskManager.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// L'indirizzo base dell'API viene letto da wwwroot/appsettings.json
// (e, se esistesse, da appsettings.Development.json) — così, se un giorno
// l'API cambia porta o viene pubblicata online, basta modificare quel file,
// senza toccare il codice C#.
var apiBaseUrl = builder.Configuration["ApiBaseUrl"]
    ?? throw new InvalidOperationException("Impostazione 'ApiBaseUrl' non trovata in appsettings.json");

// Blazored.LocalStorage: ci dà accesso al localStorage del browser da C#,
// senza dover scrivere JavaScript a mano (usa JS Interop internamente).
builder.Services.AddBlazoredLocalStorage();

// L'handler che aggiunge automaticamente "Authorization: Bearer <token>"
// a ogni richiesta (vedi AuthorizationMessageHandler.cs).
builder.Services.AddTransient<AuthorizationMessageHandler>();

// L'HttpClient usato per parlare con TaskManager.Api, con l'handler sopra
// collegato tramite AddHttpMessageHandler.
builder.Services.AddHttpClient("Api", client => client.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<AuthorizationMessageHandler>();

// Quando nel codice si chiede semplicemente "HttpClient" (senza nome), Blazor
// deve sapere quale restituire: gli diciamo di usare sempre quello "Api"
// appena configurato, così ogni servizio può iniettare HttpClient senza
// ripetere ovunque CreateClient("Api").
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("Api"));

// Abilita il sistema di autorizzazione di Blazor (necessario per [Authorize]
// e <AuthorizeView> dentro i componenti Razor).
builder.Services.AddAuthorizationCore();

// Il nostro AuthenticationStateProvider personalizzato (legge il JWT salvato
// nel browser e ne ricava i claims dell'utente loggato).
builder.Services.AddScoped<ApiAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<ApiAuthenticationStateProvider>());

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ProjectService>();

await builder.Build().RunAsync();
