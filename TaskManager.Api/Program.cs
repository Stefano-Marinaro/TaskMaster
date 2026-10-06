using System.Text;
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

builder.Services.AddControllers(); //abilita i controller per gestire le richieste HTTP
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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection(); //redireziona le richieste HTTP a HTTPS
app.UseAuthentication(); //verifica il token JWT presente nella richiesta e popola HttpContext.User
app.UseAuthorization();
app.MapControllers(); // mappa le richieste HTTP ai controller

app.Run();

//Migration un file generato automaticamente da EF Core che descrive le istruzioni 
//SQL necessarie per creare le tabelle nel DB basandosi sulle mie classi e sul DbContext 
