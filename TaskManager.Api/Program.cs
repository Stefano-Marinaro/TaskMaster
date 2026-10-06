using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TaskManager.Api.Data;
using TaskManager.Api.Services;

var builder = WebApplication.CreateBuilder(args);

//metodo di registrazione di un DbContext nella Dependency Injection
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

//Builder, un oggetto che raccoglie insime tutta
// la configurazione dell'app prima che venga avviata
//args, rappresenta gli argomenti della riga di comando passati all'applicazione

// Add services to the container.

builder.Services.AddControllers() //abilita i controller per gestire le richieste HTTP
    .AddJsonOptions(options =>
        //Per difetto, System.Text.Json serializza un enum (es. UserRole) come il suo
        //valore numerico sottostante ("role":1), illeggibile per chi consuma l'API
        //(e fragile: se l'ordine dei valori nell'enum cambiasse, il significato dei
        //numeri già salvati cambierebbe con esso). JsonStringEnumConverter lo
        //serializza invece come il nome testuale del valore ("role":"Member").
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddSwaggerGen(); //usa le informazioni raccolte da APIExplorer per costruire il documento OpenApi, che swagger UI userà per disegnare l'interfaccia
builder.Services.AddEndpointsApiExplorer(); //ispeziona i controller per capire quali endpoint esistono
builder.Services.AddSingleton<PasswordService>();
builder.Services.AddSingleton<TokenService>();

//Configura le JwtSettings leggendole dalla sezione "Jwt" di appsettings.json
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()!;

//Configura l'autenticazione basata su token JWT
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidateAudience = true,
        ValidAudience = jwtSettings.Audience,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

//Configura il CORS (Cross-Origin Resource Sharing): per difetto, un browser blocca
//le richieste JavaScript/WebAssembly fatte da un'origine (protocollo+dominio+porta)
//verso un'origine diversa. Il frontend Blazor (es. https://localhost:7159) e questa
//API (es. https://localhost:7076) sono due origini diverse pur girando sulla stessa
//macchina: senza questa policy esplicita, ogni chiamata dal client verrebbe rifiutata
//dal browser ancora prima che arrivi al server.
const string ClientCorsPolicy = "ClientCorsPolicy";
builder.Services.AddCors(options =>
{
    options.AddPolicy(ClientCorsPolicy, policy =>
    {
        policy.WithOrigins("https://localhost:7159", "http://localhost:5097") // indirizzi di TaskManager.Client in sviluppo
              .AllowAnyHeader()   // serve per lasciar passare anche l'header "Authorization: Bearer ..."
              .AllowAnyMethod();  // GET, POST, PUT, DELETE
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection(); //redireziona le richieste HTTP a HTTPS
app.UseCors(ClientCorsPolicy); //applica la policy CORS definita sopra - va PRIMA di Authentication/Authorization
app.UseAuthentication(); //verifica il token JWT presente nella richiesta e popola HttpContext.User
app.UseAuthorization();
app.MapControllers(); // mappa le richieste HTTP ai controller

app.Run();

//Migration un file generato automaticamente da EF Core che descrive le istruzioni 
//SQL necessarie per creare le tabelle nel DB basandosi sulle mie classi e sul DbContext 
