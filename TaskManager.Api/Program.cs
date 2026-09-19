using Microsoft.EntityFrameworkCore;
using TaskManager.Api.Data;

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
builder.Services. AddEndpointsApiExplorer(); //ispeziona i controller per capire quali endpoint esistono

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection(); //redireziona le richieste HTTP a HTTPS
app.UseAuthorization();
app.MapControllers(); // mappa le richieste HTTP ai controller

app.Run();

//Migration un file generato automaticamente da EF Core che descrive le istruzioni 
//SQL necessarie per creare le tabelle nel DB basandosi sulle mie classi e sul DbContext 
