# TaskMaster — README di percorso

Riepilogo cronologico e teorico di tutto quello che è stato costruito e imparato.

Questo documento ripercorre, in ordine cronologico, tutto il percorso di apprendimento e sviluppo di **TaskMaster**: un task manager backend costruito con C#, ASP.NET Core Web API ed Entity Framework Core, pensato come progetto personale da portfolio per colloqui di lavoro.

L'obiettivo di questo file è essere un riferimento autosufficiente: rileggendolo, si dovrebbe poter ricostruire il ragionamento dietro ogni scelta, senza dover richiedere nuovamente le stesse spiegazioni.

---

## 0. Perché nasce il progetto

TaskMaster nasce come progetto personale di apprendimento, con l'obiettivo di costruire una base solida di sviluppo backend in C#: Entity Framework Core, ASP.NET Core, interfacce, programmazione asincrona — applicati a un caso concreto (un task manager) invece che a esercizi isolati e scollegati tra loro.

È pensato anche come elemento di portfolio da mostrare in colloqui di lavoro: il dominio scelto è volutamente riconoscibile e comune nel mondo del lavoro, e tocca argomenti richiesti spesso in un colloquio backend junior/mid, come autenticazione, gestione dei ruoli, relazioni tra entità e API REST.

**Cos'è TaskMaster, nel dettaglio:** un'applicazione che permette a un team di organizzare il proprio lavoro in `Project`, suddivisi in `TaskItem` assegnabili ai membri del team, con la possibilità di lasciare `Comment` su ciascuna attività.

---

## 1. Setup iniziale del progetto

### 1.1 Creazione del progetto in Visual Studio

Template usato: **ASP.NET Core Web API** (non MVC/Razor Pages/Blazor)

Perché "Web API" e non altri template:
- MVC/Razor Pages generano anche l'HTML del frontend (pagine complete)
- Blazor permette di scrivere il frontend in C#
- Web API restituisce SOLO dati (JSON), pensata per essere "consumata" da qualcos'altro: un'app React, mobile, o un frontend separato (nel nostro caso, un frontend Blazor WebAssembly — vedi sezione 10)

Questa scelta tiene la logica di business separata e "pulita" dal frontend, permettendo di costruirci sopra qualsiasi client in futuro.

Configurazione scelta in fase di creazione:
- Framework: .NET (versione più recente disponibile)
- Autenticazione: nessuna (gestita manualmente più avanti con JWT)
- Controller classici (non Minimal API) — più semplici da seguire imparando
- Swagger/OpenAPI abilitato per testare gli endpoint via browser

### 1.2 Differenza tra Layered Architecture, Vertical Slice e DDD

- **Layered Architecture**: organizza il codice per RESPONSABILITÀ TECNICA (un livello per il dominio, uno per l'accesso dati, uno per le API). È l'approccio più "classico" e insegnato, quello scelto per TaskMaster in versione semplificata (tutto in un solo progetto, senza suddivisione in progetti .NET separati).
- **Vertical Slice Architecture**: organizza il codice per FEATURE (tutto ciò che serve per una singola funzionalità sta insieme), più moderno, usato spesso con le Minimal API.
- **DDD (Domain-Driven Design)**: livello concettuale più alto, riguarda come si modella il dominio di business stesso (entità con comportamenti propri, non solo dati). Compatibile sia con Layered che con Vertical Slice.

### 1.3 Repository Git / GitHub

- Repository creata su GitHub, vuota (senza README/gitignore iniziali, generati poi in locale)
- `.gitignore` generato con `dotnet new gitignore` (esclude `bin/`, `obj/`, `.vs/`)
- Flusso base usato: `git init`, `git add .`, `git commit`, `git remote add origin`, `git branch -M main`, `git push -u origin main`
- Autenticazione verso GitHub tramite Personal Access Token (PAT)

---

## 2. Program.cs — il punto di ingresso dell'applicazione

`Program.cs` è il file da cui tutto parte. Le istruzioni vengono eseguite **in ordine dall'alto verso il basso** (a differenza delle classi normali).

Struttura logica, in due fasi nettamente separate:

**FASE 1 — Registrazione dei servizi** (prima di `Build()`)

```csharp
builder.Services.AddControllers();          // abilita i controller
builder.Services.AddEndpointsApiExplorer();  // ispeziona i controller per generare la documentazione
builder.Services.AddSwaggerGen();            // genera il documento OpenAPI
builder.Services.AddDbContext<AppDbContext>(...); // registra EF Core
builder.Services.AddSingleton<PasswordService>();  // registra i servizi custom
```

Questa fase si chiama "Dependency Injection" (vedi sezione 6).

**FASE 2 — Configurazione della pipeline HTTP** (dopo `Build()`)

```csharp
var app = builder.Build(); // IL CONFINE tra le due fasi: da qui in poi non si possono più registrare servizi

if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }
app.UseHttpsRedirection();  // forza HTTPS
app.UseAuthorization();     // controlla i permessi (pronto per JWT)
app.MapControllers();       // instrada le richieste ai controller giusti
app.Run();                  // avvia il server, resta in ascolto
```

> **Nota storica**: il template più recente di .NET (9+) usa di default `Microsoft.AspNetCore.OpenApi` (`AddOpenApi()`/`MapOpenApi()`) invece di Swagger UI classico — genera solo il JSON OpenAPI, senza interfaccia grafica. Per avere l'interfaccia grafica navigabile ("Try it out") è stato installato il pacchetto NuGet `Swashbuckle.AspNetCore` e sostituito con `AddSwaggerGen()`/`UseSwaggerUI()`.

**Note pratiche di ambiente:**
- Il certificato HTTPS di sviluppo (ASP.NET Core dev cert) va accettato la prima volta: `dotnet dev-certs https --trust`
- `dotnet run` da terminale non apre il browser automaticamente e può usare solo HTTP (profilo diverso da quello usato da F5 in Visual Studio). `dotnet watch run` ricompila e riavvia automaticamente ad ogni salvataggio, ed è generalmente preferibile durante lo sviluppo attivo.
- Senza pagina di avvio configurata, va aggiunto manualmente `/swagger` in fondo all'URL per raggiungere l'interfaccia (es. `https://localhost:7076/swagger`)

---

## 3. Il modello dati (`Models/`)

Ogni classe qui descrive SOLO la forma dei dati (proprietà) — nessuna logica, nessun accesso HTTP o database. Sono oggetti passivi.

### 3.1 `TaskItem.cs`

```
Id (int)
Title (string)
Description (string?)               <- opzionale, nullable
Status (enum TaskStatus: ToDo, InProgress, Done)
CreatedAt (DateTime)
ReferenceProjectId (int)            <- FK obbligatoria verso Project
Project? (navigation property)
AssignedUserId (int?)               <- FK opzionale verso User
AssignedUser? (navigation property)
```

> **Nota**: l'enum è stato chiamato `TaskStatus` ma esiste già un `TaskStatus` nel namespace `System.Threading.Tasks` — possibile fonte di ambiguità futura.

### 3.2 `Project.cs`

```
Id (int), Name (string), Description (string?), CreatedAt (DateTime)
Tasks: List<TaskItem>   <- relazione UNO-A-MOLTI (un progetto ha molti task)
Members: List<User>     <- relazione MOLTI-A-MOLTI (con User.Projects)
```

### 3.3 `User.cs`

```
Id (int), UserName (string), Email (string), PasswordHash (string)
Role (enum UserRole: Owner, Member)
Projects: List<Project>       <- relazione MOLTI-A-MOLTI (con Project.Members)
AssignedTasks: List<TaskItem> <- relazione UNO-A-MOLTI (lato "molti")
```

### 3.4 `Comment.cs`

```
Id (int), Content (string), CreatedAt (DateTime)
TaskItemId (int) + TaskItem? (navigation property)
AuthorId (int) + Author? (navigation property)
```

### 3.5 Concetti chiave sulle relazioni

**Foreign Key** (es. `TaskItemId`): è un semplice `int`, diventa una COLONNA nel database. Rappresenta "l'Id della riga collegata".

**Navigation Property** (es. `TaskItem`): NON è una colonna nel database. È una comodità di EF Core/C# che permette di "navigare" da un oggetto all'altro in memoria senza scrivere query manuali. Va sempre nominata come la Foreign Key meno il suffisso "Id" (`TaskItemId` → `TaskItem`), è la convenzione che permette a EF Core di riconoscere automaticamente il collegamento.

**Shadow Property**: se non si dichiara esplicitamente una Foreign Key ma si dichiara solo la collezione dal lato opposto (es. `Project.Tasks` senza `TaskItem.ProjectId`), EF Core la crea comunque in automatico nel database, ma con un nome generico (es. "ProjectId" invece di uno scelto consapevolmente) e sempre nullable. Meglio dichiarare sempre le FK esplicitamente per controllare nome e nullabilità.

**Molti-a-molti**: quando due classi hanno ciascuna una `List<>` dell'altra (come `User.Projects` e `Project.Members`), EF Core capisce automaticamente la relazione e genera da solo una tabella di collegamento nascosta (nel nostro caso `ProjectUser`), senza bisogno di scriverla a mano.

---

## 4. `Data/AppDbContext.cs` — il ponte verso il database

Il `DbContext` è la classe centrale di Entity Framework Core: rappresenta la connessione/sessione di lavoro col database. Fa da "traduttore" tra le classi C# (`Models/`) e le tabelle SQL vere.

```csharp
public class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options)
{
    public DbSet<TaskItem> TaskItems { get; set; }
    public DbSet<Project> Projects { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Comment> Comments { get; set; }
}
```

Ogni `DbSet<T>` rappresenta UNA TABELLA — tramite essa si fanno tutte le query e le operazioni di scrittura verso quella tabella.

> **Importante**: le proprietà `DbSet` devono essere `public` — EF Core le scopre tramite reflection (ispezionando le proprietà pubbliche della classe), quindi se restano private non vengono viste e le tabelle non vengono create.

**Connessione al database:**
- Pacchetti NuGet installati: `Microsoft.EntityFrameworkCore.SqlServer`, `Microsoft.EntityFrameworkCore.Tools`
- Stringa di connessione in `appsettings.json`, sotto `ConnectionStrings`:
  ```json
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=TaskMasterDb;Trusted_Connection=True;TrustServerCertificate=True"
  ```
  (LocalDB = versione leggera di SQL Server, inclusa con Visual Studio, pensata per lo sviluppo locale)
- Registrazione in `Program.cs` (prima di `Build()`):
  ```csharp
  builder.Services.AddDbContext<AppDbContext>(options =>
      options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
  ```

**Migrations** (creazione/evoluzione dello schema del database):

| Comando | Effetto |
|---|---|
| `dotnet ef migrations add NomeMigrazione` | genera i file C# che descrivono le modifiche allo schema |
| `dotnet ef migrations remove` | rimuove l'ultima migration non ancora applicata |
| `dotnet ef database update` | applica davvero le migration al database (crea/modifica tabelle) |

Verifica del risultato: **SQL Server Object Explorer** in Visual Studio (Visualizza → SQL Server Object Explorer), per vedere le tabelle create dentro `TaskMasterDb`.

---

## 5. Async/await e LINQ

### 5.1 Sincrono vs Asincrono

**Sincrono**: ogni istruzione viene eseguita una dopo l'altra, il programma resta bloccato ad aspettare che ciascuna finisca (es. una query al database blocca tutto il thread finché non arriva risposta).

**Asincrono**: operazioni di I/O (rete, database, disco) possono essere eseguite senza bloccare il thread. Mentre l'operazione è in corso "altrove" (nel database, nella rete, gestita dal sistema operativo), il thread C# viene liberato per fare altro (es. gestire un'altra richiesta HTTP). Quando l'operazione esterna finisce, il metodo riprende da dove si era fermato.

Fondamentale in un backend web: permette di gestire molte richieste contemporanee con pochi thread, invece di bloccarli in attesa del database.

### 5.2 `async` e `await`

- **`async`**: modificatore sulla firma di un metodo, abilita l'uso di `await` al suo interno.
- **`await`**: si mette prima di una chiamata a un'operazione asincrona, "mette in pausa" SOLO quel metodo (non l'intero programma/thread) finché l'operazione non è completata, poi riprende con il risultato disponibile.
- **`Task`**: rappresenta "un'operazione in corso, di cui non si conosce ancora l'esito, ma che prima o poi finirà".
- **`Task<T>`**: come `Task`, ma l'operazione produrrà un valore di tipo `T` al termine. Un metodo `async` che restituisce (con `return`) un valore `T` deve dichiarare come tipo di ritorno `Task<T>`, non `T` direttamente.

### 5.3 LINQ e query "pigre" (lazy)

Una query EF Core (es. `_context.Projects.Where(...)`) NON viene eseguita subito — è solo una descrizione di cosa si vuole ottenere. Viene eseguita davvero solo quando si chiama un metodo di "materializzazione" (`ToList()`, `First()`, `Count()`, ecc.).

Le versioni ASINCRONE di questi metodi hanno lo stesso nome + suffisso "Async" (convenzione diffusa in tutto .NET): `ToListAsync()`, `FirstAsync()`, `FindAsync()`, `AddAsync()`, `SaveChangesAsync()`.

- **`AddAsync(...)`** → traccia una modifica (non scrive nulla sul disco, come mettere qualcosa nel carrello della spesa)
- **`SaveChangesAsync()`** → scrive DAVVERO le modifiche in sospeso nel database (il "checkout"). Senza questa chiamata, `Add`/`Remove`/modifiche alle proprietà non hanno alcun effetto reale sul database.

Separare "traccia la modifica" da "salva davvero" permette di accumulare più modifiche e applicarle tutte insieme in un'unica operazione atomica (o tutto va a buon fine, o niente viene scritto) — importante quando un'operazione logica coinvolge più modifiche correlate.

---

## 6. Dependency Injection (DI)

**Problema che risolve**: invece di far CREARE le proprie dipendenze a ogni classe (es. un controller che fa `new AppDbContext(...)` al suo interno), gliele si fa "iniettare" dall'esterno, già pronte all'uso. La classe si limita a dichiarare cosa le serve nel costruttore, senza sapere come o dove viene costruito.

```csharp
public class ProjectsController : ControllerBase
{
    private readonly AppDbContext _context;
    public ProjectsController(AppDbContext context) { _context = context; }
}
```

**Vantaggi**: la classe non deve conoscere configurazione/dettagli costruttivi delle sue dipendenze; è facile sostituire un'implementazione con un'altra (es. per i test); si evita di duplicare la logica di creazione ovunque.

Il "contenitore DI" in ASP.NET Core è integrato di serie (non serve installare nulla) — è `builder.Services`, popolato in `Program.cs` con tutte le chiamate `Add...()`.

**Lifetime dei servizi** (quanto a lungo vive un'istanza):

| Metodo | Durata |
|---|---|
| `AddTransient<T>()` | nuova istanza OGNI VOLTA che viene richiesta |
| `AddScoped<T>()` | UNA istanza per l'intera durata di UNA richiesta HTTP (usato di default da `AddDbContext`) |
| `AddSingleton<T>()` | UNA sola istanza per TUTTA la vita dell'applicazione, condivisa da tutte le richieste di tutti gli utenti (adatto a servizi "stateless", senza stato mutevole interno, es. `PasswordService`) |

---

## 7. `Controllers/` — gli endpoint HTTP (CRUD completo)

Un Controller riceve le richieste HTTP e risponde, usando i servizi iniettati (`AppDbContext` e altri) per leggere/scrivere dal database.

Struttura base di ogni controller:

```csharp
[ApiController]                 // abilita validazione automatica, risposte JSON
[Route("api/[controller]")]     // URL base (es. api/Projects)
public class XController : ControllerBase
{
    private readonly AppDbContext _context;
    public XController(AppDbContext context) { _context = context; }
    // ...
}
```

### 7.1 Pattern CRUD applicato a Project, TaskItem, User, Comment

**GET (lista)** — `[HttpGet]`
`Task<List<T>>` — restituisce `_context.Set.ToListAsync()`, nessuna scrittura

**GET (singolo)** — `[HttpGet("{id}")]`
`Task<ActionResult<T>>` — usa `FindAsync(id)`, poi:
```csharp
return item == null ? NotFound() : Ok(item);
```
(operatore ternario: `condizione ? valoreSeVero : valoreSeFalso`)

**POST (crea)** — `[HttpPost]`
`Task<ActionResult<T>>` — `AddAsync(nuovoOggetto)` + `await SaveChangesAsync()`, poi risposta con `CreatedAtAction`:
```csharp
return CreatedAtAction(nameof(GetById), new { id = nuovoOggetto.Id }, nuovoOggetto);
```
`CreatedAtAction` costruisce una risposta 201 Created con header `Location` che punta all'endpoint GET singolo per recuperare la risorsa appena creata. **Importante**: `nameof(...)` deve puntare al metodo GET (che LEGGE la risorsa), mai al metodo POST stesso.

**PUT (aggiorna)** — `[HttpPut("{id}")]`
`Task<ActionResult>` — `FindAsync(id)`, early return `NotFound()` se null, aggiornamento manuale proprietà per proprietà, `await SaveChangesAsync()`, `return NoContent()` (successo senza contenuto nel corpo della risposta)

**DELETE (elimina)** — `[HttpDelete("{id}")]`
`Task<ActionResult>` — `FindAsync(id)`, early return `NotFound()` se null, `_context.Remove(item)`, `await SaveChangesAsync()`, `return NoContent()`. Nota: un DELETE non ha corpo nella richiesta, basta l'id nell'URL — non serve nessun parametro oggetto nel metodo.

### 7.2 Status code REST corretti

| Codice | Significato |
|---|---|
| 200 OK | lettura riuscita (GET) |
| 201 Created | creazione riuscita (POST), con header Location |
| 204 No Content | operazione riuscita senza corpo di risposta (PUT, DELETE) |
| 404 Not Found | risorsa non trovata |

### 7.3 "Early return" pattern

Preferire:
```csharp
if (x == null) { return NotFound(); }
// resto del codice
```
invece di annidare tutto dentro un blocco `else` — riduce l'annidamento e migliora la leggibilità mano a mano che i controlli aumentano.

### 7.4 Attenzione alle relazioni bidirezionali in fase di serializzazione JSON

Le relazioni "che si guardano a vicenda" (es. `Project.Tasks` ↔ `TaskItem.Project`) rischiano un **ciclo infinito** quando vengono convertite in JSON, se caricate esplicitamente insieme (con `Include()`). Senza `Include()` esplicito, le Navigation Property restano `null` nelle risposte di default, quindi il problema non si manifesta finché non si comincia a caricare dati collegati — punto da tenere presente per il futuro.

---

## 8. DTO (Data Transfer Object) e sicurezza dei dati esposti

**Problema riscontrato**: restituendo direttamente un oggetto `User` nelle risposte JSON, il campo `PasswordHash` veniva esposto pubblicamente a chiunque chiamasse l'endpoint — un problema di sicurezza reale, anche trattandosi solo di un hash.

**Soluzione**: i DTO sono classi SEPARATE dal modello di dominio, che rappresentano solo i dati che si vogliono davvero esporre in un contesto specifico (in entrata o in uscita da un'API).

- Cartella: `Dtos/` (nella root del progetto, accanto a `Models/`, `Controllers/`)
- `UserDto.cs` (output) — contiene solo `Id`, `UserName`, `Email`, `Role` (NIENTE `PasswordHash`, niente collezioni)

> **Errore da non ripetere**: un DTO non deve MAI ereditare dal modello di dominio (es. `class UserDto : User`) — l'ereditarietà porterebbe con sé anche i campi che si vogliono nascondere (`PasswordHash` incluso), vanificando lo scopo del pattern. Un DTO è una classe completamente indipendente.

Mapping manuale (trasformazione da modello a DTO), con LINQ `Select()`:

```csharp
var users = await _context.Users.ToListAsync();
var userDtos = users.Select(u => new UserDto
{
    Id = u.Id,
    UserName = u.UserName,
    Email = u.Email,
    Role = u.Role
}).ToList();
```

**Distinzione input vs output:**
- In creazione (POST), il parametro in ingresso resta il modello di dominio completo (`User`) — serve la password per creare l'utente.
- In lettura (GET) e in risposta dopo la creazione, si converte sempre in DTO prima di restituire — mai esporre `PasswordHash`.
- In aggiornamento (PUT), usare come TIPO del parametro in ingresso un DTO che non contiene i campi sensibili/pericolosi da modificare liberamente (es. `PasswordHash`, `Role`) — una precauzione STRUTTURALE, non solo una scelta nel codice del controller: se il tipo non prevede quel campo, il client non può nemmeno provare a mandarlo per quella via.

**Motivo per cui `Role` non va reso liberamente modificabile da un endpoint aperto**: senza autorizzazione, chiunque potrebbe autopromuoversi da `Member` a `Owner` semplicemente chiamando l'endpoint — problema che l'autenticazione/autorizzazione (JWT + ruoli) è pensata per risolvere.

---

## 9. Autenticazione JWT (completata)

**Punto in cui si è arrivati**: hashing delle password, registrazione, login con generazione del token, validazione del token sulle richieste successive e protezione degli endpoint con `[Authorize]` sono tutti completati e funzionanti.

### 9.1 Il problema che risolve

Finora chiunque può chiamare qualsiasi endpoint, senza restrizioni — non c'è modo per il server di sapere CHI sta facendo una richiesta.
- **Autenticazione**: risponde a "chi sei?"
- **Autorizzazione**: risponde a "cosa ti è permesso fare, dato chi sei?"

### 9.2 Flusso concettuale generale

1. **Registrazione**: l'utente crea un account, la password viene HASHATA (mai salvata in chiaro).
2. **Login**: l'utente manda le credenziali, il server verifica l'hash e, se corretto, genera e restituisce un token JWT.
3. **Richieste successive**: il client allega il token ad ogni richiesta (header `Authorization`), il server lo legge e sa subito chi sta chiamando, senza bisogno di richiedere di nuovo le credenziali.

### 9.3 Struttura di un JWT

```
header.payload.signature
```

- **Header**: algoritmo di firma usato
- **Payload**: dati sull'utente (claims) — `UserId`, `UserName`, `Role`, scadenza. Leggibili da chiunque (solo codificati in Base64, NON cifrati) — mai mettere dati sensibili qui dentro.
- **Signature**: firma crittografica generata dal server con una chiave segreta nota solo al server — garantisce che il token non sia stato manomesso.

Il server è "stateless": non deve ricordare nulla sull'utente tra una richiesta e l'altra, tutto ciò che serve è già dentro il token, verificabile con la sola chiave segreta.

### 9.4 Hashing delle password — `PasswordService`

Non si scrive mai un proprio algoritmo di hashing — si usa una libreria testata: `Microsoft.AspNetCore.Identity`, classe `PasswordHasher<T>`.

- Pacchetto NuGet: `Microsoft.AspNetCore.Identity`
- Cartella: `Services/`
- File: `PasswordService.cs`

```csharp
public class PasswordService
{
    private readonly PasswordHasher<User> _hasher = new();

    public string HashPassword(User user, string plainPassword)
        => _hasher.HashPassword(user, plainPassword);

    public bool VerifyPassword(User user, string hashedPassword, string plainPassword)
    {
        var result = _hasher.VerifyHashedPassword(user, hashedPassword, plainPassword);
        return result == PasswordVerificationResult.Success;
    }
}
```

Registrato in `Program.cs` come Singleton (nessuno stato mutevole interno):
```csharp
builder.Services.AddSingleton<PasswordService>();
```

### 9.5 `RegisterDto` e `LoginDto` — i dati in ingresso

- Cartella: `Dtos/`

```csharp
// RegisterDto.cs
public class RegisterDto
{
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;  // password in chiaro, MAI PasswordHash
}
```

Motivo: il client non deve mai calcolare l'hash da solo — manda la password in chiaro (su connessione HTTPS), e il server la hasha prima di salvarla.

```csharp
// LoginDto.cs
public class LoginDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
```

Per il login basta l'identificativo (email) e la password in chiaro: il server ritrova l'utente, rifà l'hash della password ricevuta e lo confronta con quello salvato (non si "decifra" mai un hash, si verifica per confronto — vedi 9.4).

### 9.6 `AuthResponseDto` — i dati in uscita dal login

- Cartella: `Dtos/`
- File: `AuthResponseDto.cs`

```csharp
public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public UserDto User { get; set; } = null!;
}
```

Al login il client riceve non solo il token, ma anche la sua scadenza (utile al frontend per sapere quando richiedere un nuovo login) e i dati pubblici dell'utente (`UserDto`, senza `PasswordHash`) — evita una seconda chiamata a `GET /api/users/{id}` subito dopo il login solo per sapere "chi sono".

### 9.7 `JwtSettings` — la configurazione del token

- Cartella: `Services/`
- File: `JwtSettings.cs`

```csharp
public class JwtSettings
{
    public string Key { get; set; } = string.Empty;        // chiave segreta per firmare/verificare il token
    public string Issuer { get; set; } = string.Empty;     // "chi" ha emesso il token (la nostra API)
    public string Audience { get; set; } = string.Empty;   // "per chi" è pensato il token (il nostro client)
    public int ExpireMinutes { get; set; } = 60;            // durata di validità, in minuti
}
```

Questa classe è un semplice contenitore (POCO — Plain Old C# Object) che rispecchia la struttura della sezione `Jwt` dentro `appsettings.json`:

```json
"Jwt": {
  "Key": "<stringa lunga e segreta, generata a caso>",
  "Issuer": "TaskManager.Api",
  "Audience": "TaskManager.Client",
  "ExpireMinutes": 60
}
```

Il collegamento tra il JSON e la classe C# avviene tramite il pattern **Options** di ASP.NET Core (`IOptions<T>`), configurato in `Program.cs`:

```csharp
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
```

> **Sicurezza — la chiave NON va mai committata in chiaro**: in `appsettings.json` il campo `Jwt:Key` contiene solo un placeholder descrittivo, non il valore reale. Il valore vero è salvato SOLO in locale con i **.NET User Secrets** (fuori dalla cartella del progetto, quindi non tracciato da Git né mai inviato a GitHub):
> ```bash
> dotnet user-secrets init                               # crea un UserSecretsId nel .csproj (quello sì va committato, non è un segreto)
> dotnet user-secrets set "Jwt:Key" "<chiave-lunga-e-casuale>"
> ```
> `WebApplication.CreateBuilder(args)` carica automaticamente i User Secrets quando l'ambiente è `Development`, con priorità più alta di `appsettings.json` — quindi il valore reale sovrascrive il placeholder a runtime, senza bisogno di nessuna configurazione aggiuntiva in `Program.cs`. In produzione (dove i User Secrets non si usano) la stessa chiave andrebbe fornita con una variabile d'ambiente (`Jwt__Key`, doppio underscore per i livelli annidati) o un servizio di secret management.

### 9.8 `TokenService` — generazione del token JWT

- Cartella: `Services/`
- File: `TokenService.cs`
- Pacchetto NuGet aggiuntivo: `Microsoft.AspNetCore.Authentication.JwtBearer` (porta con sé anche le classi per CREARE un token, non solo per validarlo)

```csharp
public class TokenService
{
    private readonly JwtSettings _settings;

    public TokenService(IOptions<JwtSettings> options)
    {
        _settings = options.Value;   // IOptions<T> espone la configurazione già "risolta" tramite .Value
    }

    public string CreateToken(User user)
    {
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_settings.ExpireMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public DateTime GetExpiration() => DateTime.UtcNow.AddMinutes(_settings.ExpireMinutes);
}
```

Concetti chiave:
- **Claim**: una singola affermazione su chi è l'utente ("il suo Id è 5", "il suo ruolo è Owner"). Diventano il contenuto del payload del token (vedi 9.3) — leggibili da chiunque, quindi niente dati sensibili (mai la password, nemmeno hashata).
- **SymmetricSecurityKey**: la chiave segreta trasformata in un formato utilizzabile per firmare. "Simmetrica" perché la STESSA chiave serve sia per firmare (al login) che per verificare (nelle richieste successive) — per questo deve restare segreta e conosciuta solo dal server.
- **SigningCredentials**: abbina la chiave a un algoritmo di firma specifico (qui `HmacSha256`).
- **`JwtSecurityTokenHandler().WriteToken(...)`**: serializza tutto (header + payload + signature) nella stringa finale `xxxxx.yyyyy.zzzzz` che viene restituita al client.

Registrato in `Program.cs` come Singleton (nessuno stato mutevole interno, come `PasswordService`):
```csharp
builder.Services.AddSingleton<TokenService>();
```

### 9.9 `AuthController` — endpoint `register` e `login`

- Cartella: `Controllers/`
- File: `AuthController.cs`
- Route: `api/auth`

Il costruttore ora riceve TRE servizi:

```csharp
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly PasswordService _passwordService;
    private readonly TokenService _tokenService;

    public AuthController(AppDbContext context, PasswordService passwordService, TokenService tokenService)
    {
        _context = context;
        _passwordService = passwordService;
        _tokenService = tokenService;
    }
```

**`POST /api/auth/register`** — crea l'utente:
```csharp
[HttpPost("register")]
public async Task<ActionResult<UserDto>> Register(RegisterDto dto)
{
    var newUser = new User { UserName = dto.UserName, Email = dto.Email };
    newUser.PasswordHash = _passwordService.HashPassword(newUser, dto.Password);

    await _context.Users.AddAsync(newUser);
    await _context.SaveChangesAsync();

    var userDto = new UserDto { Id = newUser.Id, UserName = newUser.UserName, Email = newUser.Email, Role = newUser.Role };
    return Ok(userDto);
}
```

**`POST /api/auth/login`** — verifica le credenziali e restituisce il token:
```csharp
[HttpPost("login")]
public async Task<ActionResult<AuthResponseDto>> Login(LoginDto dto)
{
    var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
    if (user == null)
        return Unauthorized("Email o password non validi.");

    var isPasswordValid = _passwordService.VerifyPassword(user, user.PasswordHash, dto.Password);
    if (!isPasswordValid)
        return Unauthorized("Email o password non validi.");

    var token = _tokenService.CreateToken(user);

    return Ok(new AuthResponseDto
    {
        Token = token,
        ExpiresAt = _tokenService.GetExpiration(),
        User = new UserDto { Id = user.Id, UserName = user.UserName, Email = user.Email, Role = user.Role }
    });
}
```

**Dettaglio voluto**: il messaggio di errore è identico sia se l'email non esiste sia se la password è sbagliata ("Email o password non validi."). Distinguere i due casi (es. "email non trovata" vs "password sbagliata") rivelerebbe a un attaccante quali email sono registrate nel sistema — informazione che non deve mai uscire da un endpoint pubblico.

### 9.10 Configurazione in `Program.cs` — abilitare la validazione del JWT

Tre aggiunte, da fare PRIMA di `builder.Build()`:

```csharp
// 1. Collega la sezione "Jwt" di appsettings.json alla classe JwtSettings
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()!;

// 2. Registra e configura lo schema di autenticazione "Bearer" (JWT)
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
        ValidateLifetime = true,                 // rifiuta token scaduti
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
        ClockSkew = TimeSpan.Zero                 // nessun margine extra sulla scadenza
    };
});

// 3. Abilita il sistema di autorizzazione (necessario per far funzionare [Authorize])
builder.Services.AddAuthorization();
```

E una modifica DOPO `builder.Build()`, nella pipeline HTTP — **l'ordine qui è importante**:

```csharp
app.UseHttpsRedirection();
app.UseAuthentication();   // legge il token dalla richiesta e popola HttpContext.User — DEVE venire prima
app.UseAuthorization();    // controlla se HttpContext.User ha il permesso di accedere all'endpoint richiesto
app.MapControllers();
```

Se si invertisse l'ordine (`UseAuthorization()` prima di `UseAuthentication()`), il controllo dei permessi avverrebbe prima che il server sappia chi è l'utente — fallirebbe sempre o si comporterebbe in modo imprevedibile, perché `HttpContext.User` non sarebbe ancora stato popolato dal token.

### 9.11 `[Authorize]` — proteggere gli endpoint esistenti

Aggiunto l'attributo `[Authorize]` (namespace `Microsoft.AspNetCore.Authorization`) a livello di CLASSE sui controller che devono richiedere un utente autenticato, in modo che si applichi automaticamente a tutti i loro metodi:

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]   // ogni richiesta a questo controller deve avere un token JWT valido nell'header Authorization
public class ProjectsController : ControllerBase { ... }
```

Applicato a `ProjectsController`, `TasksController`, `CommentsController`, `UsersController`. **`AuthController` resta SENZA `[Authorize]`**: `register` e `login` devono necessariamente restare accessibili a chi non ha ancora un token (altrimenti nessuno potrebbe mai ottenerne uno).

Senza un token valido, una richiesta a un endpoint protetto riceve **401 Unauthorized** automaticamente, prima ancora che il codice del controller venga eseguito.

### 9.12 Come si usa dal lato client (es. da Swagger/Postman/frontend)

1. `POST /api/auth/register` con `{ "userName", "email", "password" }` → crea l'utente.
2. `POST /api/auth/login` con `{ "email", "password" }` → risponde con `{ "token", "expiresAt", "user" }`.
3. Ogni richiesta successiva a un endpoint protetto deve includere l'header:
   ```
   Authorization: Bearer <token ricevuto al login>
   ```
4. Il server valida firma, issuer, audience e scadenza del token (vedi 9.10); se tutto è valido, popola `HttpContext.User` con i claims e lascia proseguire la richiesta verso il controller.

### 9.13 Differenziazione dei permessi per ruolo (non ancora affrontata)

`[Authorize]` da solo verifica solo "sei autenticato?", non "hai il ruolo giusto?". Il passo successivo, non ancora implementato, sarebbe usare `[Authorize(Roles = "Owner")]` su endpoint specifici (es. cancellare un progetto) per restringerli a un ruolo preciso, sfruttando il claim `ClaimTypes.Role` già incluso nel token (vedi 9.8).

---

## 10. Frontend: Blazor WebAssembly (completato)

**Punto in cui si è arrivati**: il backend (API + JWT) funziona ed è stato testato. Questa sezione racconta la costruzione del frontend: un'applicazione separata che consuma l'API tramite chiamate HTTP, con login, registrazione e una pagina protetta per gestire i progetti.

### 10.1 Perché Blazor WebAssembly, e non React/Angular/Vue

Con l'API già pronta, la scelta del frontend è in realtà indipendente dal linguaggio: un'API REST può essere "consumata" da QUALSIASI tecnologia capace di fare richieste HTTP (JavaScript, mobile, un altro servizio, ecc. — vedi sezione 1.1).

Per un progetto di apprendimento in C#, però, **Blazor WebAssembly** è la scelta più coerente dal punto di vista didattico:
- resta nello stesso linguaggio (C#) e nello stesso IDE (Visual Studio) usati per il backend — non bisogna imparare contemporaneamente ASP.NET Core E un intero ecosistema JavaScript (npm, bundler, sintassi diversa) solo per vedere l'API "in azione";
- è comunque una **SPA (Single Page Application)** vera e propria, della stessa famiglia concettuale di React/Angular: gira interamente nel browser, chiama l'API in background, aggiorna la pagina senza ricaricarla — quindi i concetti imparati qui (routing lato client, stato di autenticazione, chiamate HTTP asincrone) si trasferiscono comunque a qualsiasi altro framework SPA;
- "WebAssembly" (contro l'alternativa "Blazor Server") significa che tutto il codice C# del frontend viene scaricato dal browser e **eseguito lì**, non sul server — è l'opzione che obbliga davvero a trattare il frontend come un client separato che deve autenticarsi con un token, proprio come farebbe un'app scritta in un altro linguaggio. Blazor Server, al contrario, terrebbe la UI "in vita" sul server tramite una connessione persistente (SignalR), nascondendo di fatto il problema di autenticazione via JWT che qui si vuole invece imparare a risolvere.

### 10.2 Creazione del progetto e struttura della solution

Comandi eseguiti dalla cartella che contiene già `TaskManager.Api/`:

```bash
dotnet new blazorwasm -o TaskManager.Client -n TaskManager.Client   # crea il progetto Blazor WebAssembly standalone
dotnet sln TaskManager.Api.slnx add TaskManager.Client/TaskManager.Client.csproj   # lo aggiunge alla solution esistente
```

Risultato: **due progetti indipendenti** nella stessa solution/cartella, che comunicano SOLO via HTTP (non si referenziano a vicenda come progetti .NET):

```
TaskManager.Api/            (solution)
├── TaskManager.Api/         ← backend: ASP.NET Core Web API (sezioni 1-9)
└── TaskManager.Client/      ← frontend: Blazor WebAssembly (questa sezione)
```

> **Scelta didattica consapevole — niente progetto condiviso**: i DTO del frontend (`TaskManager.Client/Models/`) sono copie scritte a mano delle classi equivalenti lato server (`TaskManager.Api/Dtos/`), non lo stesso file condiviso tramite un terzo progetto (`TaskManager.Shared`, una pratica comune in progetti Blazor reali). La duplicazione è voluta in questa fase: tiene i due progetti completamente indipendenti e più facili da seguire separatamente mentre si imparano i concetti; va segnalata come primo miglioramento strutturale futuro (sezione 11).

### 10.3 Il problema del CORS

Avviando entrambi i progetti in locale, girano su **origini diverse** (stesso computer, ma protocollo/porta diversi contano come origine diversa agli occhi del browser):
- API: `https://localhost:7076`
- Client: `https://localhost:7159`

Per difetto, un browser blocca qualsiasi richiesta che il codice JavaScript/WebAssembly di una pagina prova a fare verso un'origine diversa da quella da cui la pagina stessa è stata caricata (politica di sicurezza chiamata **Same-Origin Policy**) — altrimenti una qualunque pagina web potrebbe silenziosamente fare richieste verso i siti bancari dell'utente usando i suoi cookie. **CORS (Cross-Origin Resource Sharing)** è il meccanismo con cui un server dichiara esplicitamente "fidati, queste altre origini possono chiamarmi".

Configurato in `TaskManager.Api/Program.cs`:

```csharp
const string ClientCorsPolicy = "ClientCorsPolicy";
builder.Services.AddCors(options =>
{
    options.AddPolicy(ClientCorsPolicy, policy =>
    {
        policy.WithOrigins("https://localhost:7159", "http://localhost:5097")
              .AllowAnyHeader()   // serve per lasciar passare anche "Authorization: Bearer ..."
              .AllowAnyMethod();  // GET, POST, PUT, DELETE
    });
});
```

E attivato nella pipeline, **prima** di `UseAuthentication`/`UseAuthorization` (l'ordine conta, come già visto in 9.10):

```csharp
app.UseHttpsRedirection();
app.UseCors(ClientCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();
```

> Da tenere a mente: `WithOrigins(...)` elenca indirizzi fissi, validi solo in sviluppo in locale. Se un giorno il frontend verrà pubblicato online con un suo dominio, la policy andrà aggiornata con quel dominio reale.

### 10.4 I modelli lato client (`Models/`)

Stessa idea dei DTO lato server (sezione 8), applicata al frontend: classi che descrivono SOLO la forma dei dati scambiati con l'API via JSON, senza nessuna logica.

```csharp
// Models/AuthModels.cs
public class LoginModel
{
    [Required(ErrorMessage = "L'email è obbligatoria.")]
    [EmailAddress(ErrorMessage = "Inserisci un'email valida.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "La password è obbligatoria.")]
    public string Password { get; set; } = string.Empty;
}
```

Novità rispetto ai DTO lato server: gli attributi **`[DataAnnotations]`** (`[Required]`, `[EmailAddress]`, `[MinLength]`). Blazor li legge automaticamente dentro un `<EditForm>` (sezione 10.10) per validare i campi **nel browser, prima ancora di contattare il server** — un feedback immediato per l'utente. Il server, però, convalida comunque i propri input in autonomia: non ci si fida mai dei soli controlli lato client, che un utente smaliziato potrebbe disattivare modificando il codice della pagina.

### 10.5 `HttpClient` "con nome" e il `DelegatingHandler` — allegare il token automaticamente

Ogni chiamata a un endpoint protetto deve portare l'header `Authorization: Bearer <token>` (sezione 9.11). Scriverlo a mano in OGNI chiamata, in OGNI pagina, sarebbe facile da sbagliare e da dimenticare — la soluzione è un **`DelegatingHandler`**: una classe che si inserisce automaticamente nel percorso di ogni richiesta fatta da un certo `HttpClient`, e può modificarla prima che parta.

```csharp
// Services/AuthorizationMessageHandler.cs
public class AuthorizationMessageHandler : DelegatingHandler
{
    private readonly ILocalStorageService _localStorage;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await _localStorage.GetItemAsync<string>("authToken", cancellationToken);
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        return await base.SendAsync(request, cancellationToken);   // passa la richiesta avanti
    }
}
```

Collegato in `Program.cs` a un `HttpClient` registrato "con nome" (`"Api"`), tramite `AddHttpMessageHandler`:

```csharp
builder.Services.AddTransient<AuthorizationMessageHandler>();

builder.Services.AddHttpClient("Api", client => client.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<AuthorizationMessageHandler>();

// Così ogni servizio può semplicemente chiedere "dammi un HttpClient" senza
// specificare ogni volta il nome "Api":
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("Api"));
```

Risultato: **ogni** richiesta fatta da quell'`HttpClient` (login escluso, perché lì non esiste ancora un token da allegare) passa automaticamente dall'handler, che aggiunge l'header se un token è presente. Nessuna pagina Razor deve più occuparsene.

### 10.6 Dove vive il token nel browser — `localStorage`

Il token deve "sopravvivere" alla chiusura/ricarica della pagina (altrimenti l'utente dovrebbe rifare login ogni volta) — una semplice variabile C# in memoria non basta, perché Blazor WebAssembly rilancerebbe da zero tutto il codice a ogni refresh del browser.

**`localStorage`** è uno spazio di archiviazione messo a disposizione dal browser stesso, specifico per ogni sito (origine), che resta popolato anche chiudendo la scheda. Blazor di per sé non può leggerlo/scriverlo direttamente (è un'API JavaScript del browser, non .NET) — per questo si usa il pacchetto NuGet **`Blazored.LocalStorage`**, che fa da "ponte" (JS Interop) ed espone un servizio .NET pulito:

```csharp
await _localStorage.SetItemAsync("authToken", authResponse.Token);   // scrive
var token = await _localStorage.GetItemAsync<string>("authToken");   // legge
await _localStorage.RemoveItemAsync("authToken");                    // rimuove (logout)
```

### 10.7 `ApiAuthenticationStateProvider` — insegnare a Blazor chi è l'utente loggato

Blazor ha un concetto proprio, indipendente da ASP.NET Core "server-side", per rispondere alla domanda "chi è l'utente attuale?": la classe astratta **`AuthenticationStateProvider`**. Di base, in un progetto Blazor WebAssembly standalone, non esiste nessuna implementazione pronta — va scritta.

```csharp
public class ApiAuthenticationStateProvider : AuthenticationStateProvider
{
    private static readonly ClaimsPrincipal Anonymous = new(new ClaimsIdentity());

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await _localStorage.GetItemAsync<string>("authToken");
        if (string.IsNullOrWhiteSpace(token))
        {
            return new AuthenticationState(Anonymous);
        }

        var claims = ParseClaimsFromJwt(token);               // decodifica il payload del token
        var identity = new ClaimsIdentity(claims, authenticationType: "jwt");
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    public void NotifyUserChanged()
        => NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
}
```

Concetti chiave:
- **`ClaimsPrincipal`**: la rappresentazione .NET standard di "un utente e tutto ciò che sappiamo di lui" — un insieme di `Claim` (esattamente gli stessi messi dal server nel token, sezione 9.8: Id, nome, email, ruolo).
- **Niente verifica della firma qui**: il client si limita a LEGGERE il contenuto del token (il payload è solo codificato in Base64, non cifrato — sezione 9.3), non può e non deve verificarne la firma, perché non possiede (e non deve possedere) la chiave segreta. La verifica vera e propria la fa sempre e solo il server, a ogni singola richiesta (sezione 9.10) — qui serve solo per sapere "cosa mostrare" nell'interfaccia.
- **`NotifyUserChanged()`**: un metodo custom (non fa parte della classe base) richiamato subito dopo login/logout, per dire esplicitamente a Blazor "lo stato è cambiato, ridisegna tutto quello che dipende dall'autenticazione" (NavMenu, pagine protette, ecc.).

Registrato in `Program.cs` come servizio **Scoped** sia con il proprio tipo concreto sia come `AuthenticationStateProvider` (il tipo che i componenti Blazor si aspettano di poter richiedere):
```csharp
builder.Services.AddAuthorizationCore();   // abilita il sistema di autorizzazione nei componenti Razor
builder.Services.AddScoped<ApiAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<ApiAuthenticationStateProvider>());
```

### 10.8 `AuthService` — login, registrazione, logout

Centralizza tutte le chiamate di autenticazione, così le pagine Razor restano semplici (si limitano a chiamare un metodo e mostrare il risultato):

```csharp
public async Task<string?> LoginAsync(LoginModel model)   // null = successo, altrimenti messaggio d'errore
{
    var response = await _http.PostAsJsonAsync("api/auth/login", model);
    if (!response.IsSuccessStatusCode) return "Email o password non validi.";

    var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
    await _localStorage.SetItemAsync("authToken", authResponse!.Token);
    _authStateProvider.NotifyUserChanged();   // avvisa Blazor del nuovo stato
    return null;
}
```

`RegisterAsync` è analogo ma chiama `api/auth/register` e **non** salva nessun token: coerentemente con l'API (sezione 9.9), la registrazione crea solo l'utente — il token si ottiene con un login esplicito successivo.

### 10.9 Proteggere le pagine: `CascadingAuthenticationState` e `AuthorizeRouteView`

Perché una pagina con l'attributo `[Authorize]` (lo stesso usato lato server, sezione 9.11 — ma qui applicato a un COMPONENTE Razor, non a un controller) sia davvero bloccata per chi non è loggato, il router di Blazor va configurato per controllarlo. In `App.razor`:

```razor
<CascadingAuthenticationState>
    <Router AppAssembly="@typeof(App).Assembly" NotFoundPage="typeof(Pages.NotFound)">
        <Found Context="routeData">
            <AuthorizeRouteView RouteData="@routeData" DefaultLayout="@typeof(MainLayout)">
                <NotAuthorized>
                    <p>Devi accedere per vedere questa pagina. <a href="/login">Vai al login</a>.</p>
                </NotAuthorized>
            </AuthorizeRouteView>
        </Found>
    </Router>
</CascadingAuthenticationState>
```

- **`CascadingAuthenticationState`**: rende lo stato di autenticazione (calcolato da `ApiAuthenticationStateProvider`, sezione 10.7) disponibile a TUTTI i componenti figli, a qualsiasi livello di profondità, senza doverlo passare manualmente da un componente al genitore successivo — da qui il nome "a cascata".
- **`AuthorizeRouteView`**: sostituto del semplice `RouteView` del template di base; prima di mostrare la pagina richiesta controlla se ha `[Authorize]` e, in caso affermativo, se l'utente risulta autenticato. Se non lo è, mostra il contenuto di `<NotAuthorized>` invece della pagina — il codice C# della pagina protetta non viene nemmeno eseguito.

Sulla pagina stessa, basta l'attributo in cima al file (equivalente dell'attributo su un controller):
```razor
@page "/projects"
@attribute [Authorize]
```

### 10.10 `<AuthorizeView>` — mostrare/nascondere pezzi di interfaccia

Diverso da `[Authorize]` (che blocca l'INTERA pagina): `<AuthorizeView>` permette di mostrare contenuti diversi nella STESSA pagina a seconda del login, utile per il menu di navigazione o la Home:

```razor
<AuthorizeView>
    <Authorized>
        <p>Sei autenticato come <strong>@context.User.Identity?.Name</strong>.</p>
        <a href="/projects">Vai ai tuoi progetti</a>
    </Authorized>
    <NotAuthorized>
        <p>Per iniziare, <a href="/login">accedi</a> oppure <a href="/register">crea un account</a>.</p>
    </NotAuthorized>
</AuthorizeView>
```

Usato sia in `Home.razor` sia in `Layout/NavMenu.razor` (dove mostra "Progetti"/"Esci" se loggato, "Accedi"/"Registrati" altrimenti).

### 10.11 Le pagine

| Pagina | Route | Protetta? | Cosa fa |
|---|---|---|---|
| `Home.razor` | `/` | No | Messaggio di benvenuto, link diversi in base al login (`<AuthorizeView>`) |
| `Login.razor` | `/login` | No | `<EditForm>` con email/password, chiama `AuthService.LoginAsync`, poi redirige a `/projects` |
| `Register.razor` | `/register` | No | `<EditForm>` con nome utente/email/password, chiama `AuthService.RegisterAsync` |
| `Projects.razor` | `/projects` | **Sì** (`[Authorize]`) | CRUD completo sui progetti: lista (`GET`), creazione (`POST`), modifica in linea (`PUT`), eliminazione (`DELETE`) |

`Projects.razor` ricalca lo stesso pattern CRUD già visto lato server (sezione 7), ma dal punto di vista del client — stesso concetto di "early return"/caricamento asincrono, applicato questa volta nel ciclo di vita di un componente Blazor:

```csharp
protected override async Task OnInitializedAsync()   // eseguito una volta sola, alla creazione del componente
{
    projects = await ProjectService.GetAllAsync();    // GET api/projects, con il token già allegato dall'handler
}
```

`ProjectService` (in `Services/ProjectService.cs`) è un semplice "wrapper" attorno a `HttpClient`, con lo stesso scopo del `_context` nei controller lato server: isolare le chiamate CRUD in un unico posto riutilizzabile, invece di scrivere `GetFromJsonAsync`/`PostAsJsonAsync` direttamente dentro ogni pagina.

### 10.12 Configurazione: `wwwroot/appsettings.json`

L'indirizzo dell'API non è scritto a mano nel codice C#, ma letto da un file di configurazione — esattamente come la stringa di connessione al database lato server (sezione 4):

```json
{
  "ApiBaseUrl": "https://localhost:7076/"
}
```

```csharp
var apiBaseUrl = builder.Configuration["ApiBaseUrl"]
    ?? throw new InvalidOperationException("Impostazione 'ApiBaseUrl' non trovata in appsettings.json");
```

Così, se un giorno l'API cambia porta o viene pubblicata online con un dominio reale, basta modificare questo file — nessun codice C# da ricompilare.

### 10.13 Come avviare backend e frontend insieme

Sono due applicazioni **indipendenti**: vanno avviate entrambe, in due processi separati.

- **Da Visual Studio**: tasto destro sulla solution → **Proprietà** → "Progetti di avvio multipli" → impostare sia `TaskManager.Api` sia `TaskManager.Client` su "Avvia" → F5 avvia entrambi insieme, ciascuno nel proprio terminale/finestra browser.
- **Da due terminali separati**, uno per cartella:
  ```bash
  cd TaskManager.Api && dotnet watch run
  cd TaskManager.Client && dotnet watch run
  ```

L'ordine di avvio non è rigido, ma se il frontend parte prima e si prova subito a fare login, la chiamata fallirà finché anche l'API non è pronta (il messaggio d'errore gestito in `Projects.razor`, "Impossibile contattare l'API", copre proprio questo caso).

### 10.14 Limiti noti e semplificazioni didattiche (consapevoli)

- **Nessun refresh automatico del token**: dopo 60 minuti (`Jwt:ExpireMinutes`) il token scade e le richieste protette torneranno 401 — l'utente deve rifare login manualmente. Un'app reale implementerebbe un "refresh token" per rinnovarlo in automatico.
- **DTO duplicati, non condivisi** (vedi nota in 10.2).
- **Nessuna differenziazione di permessi nella UI**: il frontend non nasconde/mostra ancora pulsanti in base al `Role` (Owner/Member) — rimanda alla sezione 9.13, non ancora affrontata nemmeno lato server.
- **CORS con indirizzi fissi**, validi solo in sviluppo locale (vedi nota in 10.3).

---

## 11. Piano futuro (non ancora affrontato)

- Differenziazione dei permessi per ruolo con `[Authorize(Roles = "...")]`, sia lato API (sezione 9.13) sia nella UI del frontend (nascondere/mostrare azioni in base al `Role`)
- Progetto `TaskManager.Shared` per eliminare la duplicazione dei DTO tra backend e frontend (sezione 10.2)
- Refresh token, per evitare che l'utente debba rifare login ogni volta che il JWT scade (sezione 10.14)
- Pagine frontend per `TaskItem` e `Comment` (oggi solo `Project` ha una pagina CRUD completa)
- Test con dati realmente collegati tra loro (Project con Task assegnati a User specifici) — verificare dal vivo il comportamento delle relazioni e il rischio di cicli di serializzazione JSON quando si useranno `Include()`
- Possibili raffinamenti futuri:
  - `OnModelCreating` per configurazioni esplicite su relazioni più complesse (es. se la relazione molti-a-molti User-Project dovesse arricchirsi di dati propri, come il ruolo dell'utente in quello specifico progetto)
  - Endpoint dedicato per spostare un `TaskItem` tra `Project` diversi, separato dal PUT generico di aggiornamento (separazione delle responsabilità)
  - Filtri/ricerca sui Task, notifiche, statistiche/dashboard
  - Pubblicazione online di backend e frontend (hosting reale, HTTPS con certificato valido, CORS con dominio vero)

---

## 12. Comandi da terminale utilizzati (in ordine cronologico)

Di seguito tutti i comandi bash/terminale incontrati durante lo sviluppo, nell'ordine in cui sono comparsi nel percorso.

### 12.1 Setup Git / GitHub

| Comando | Effetto |
|---|---|
| `dotnet new gitignore` | Genera un file `.gitignore` già pronto per progetti .NET (esclude `bin/`, `obj/`, `.vs/`), evitando di scriverlo a mano |
| `git init` | Inizializza un repository Git nella cartella corrente |
| `git add .` | Prepara (stage) tutti i file della cartella per il prossimo commit |
| `git commit -m "Initial commit: TaskManager.Api scaffold"` | Crea il primo commit con i file preparati |
| `git remote add origin <URL-repo>` | Collega il repository locale a quello remoto su GitHub |
| `git branch -M main` | Rinomina il branch principale in "main" |
| `git push -u origin main` | Invia (push) i commit locali al repository remoto su GitHub, e imposta "origin main" come destinazione predefinita per i push successivi |

### 12.2 Certificato HTTPS di sviluppo

| Comando | Effetto |
|---|---|
| `dotnet dev-certs https --trust` | Genera (se non esiste) e installa come affidabile il certificato HTTPS di sviluppo di ASP.NET Core, necessario per far funzionare HTTPS in locale su localhost senza avvisi del browser |

### 12.3 Pacchetti NuGet installati — backend (`TaskManager.Api`)

| Comando | Effetto |
|---|---|
| `dotnet add package Swashbuckle.AspNetCore` | Installa Swagger UI classico (sostituendo il template OpenAPI di default di .NET 9+), per avere l'interfaccia grafica interattiva delle API in fase di sviluppo |
| `dotnet add package Microsoft.EntityFrameworkCore.SqlServer` | Installa il provider Entity Framework Core per SQL Server, necessario per collegarsi a un database SQL Server/LocalDB |
| `dotnet add package Microsoft.EntityFrameworkCore.Tools` | Installa gli strumenti da riga di comando di Entity Framework Core (necessari per i comandi `dotnet ef ...`) |
| `dotnet add package Microsoft.AspNetCore.Identity` | Installa le classi di ASP.NET Core Identity, usate per `PasswordHasher<T>` (hashing e verifica sicura delle password) |
| `dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer` | Installa le classi per generare e validare token JWT (`JwtSecurityToken`, `TokenValidationParameters`, schema di autenticazione `Bearer`) |
| `dotnet tool install --global dotnet-ef` | (Solo se `dotnet ef` non viene riconosciuto) Installa globalmente lo strumento a riga di comando di Entity Framework Core |

### 12.4 Migrations e database (Entity Framework Core)

| Comando | Effetto |
|---|---|
| `dotnet ef migrations add InitialCreate` | Genera i file di Migration (in `Migrations/`) che descrivono le istruzioni per creare le tabelle nel database, basandosi sulle classi modello e su `AppDbContext`. Non tocca ancora il database |
| `dotnet ef migrations remove` | Rimuove l'ultima Migration generata (non ancora applicata al database), usato quando il modello cambia prima di aver eseguito `database update` |
| `dotnet ef database update` | Applica davvero le istruzioni della Migration al database, creando o modificando concretamente le tabelle |

### 12.5 .NET User Secrets (chiave JWT)

| Comando | Effetto |
|---|---|
| `dotnet user-secrets init` | Crea un `UserSecretsId` univoco nel `.csproj` (quello sì va committato: è solo un identificativo, non un segreto) e la cartella locale associata dove i secret verranno salvati |
| `dotnet user-secrets set "Jwt:Key" "<valore>"` | Salva la chiave di firma JWT fuori dal progetto (non tracciata da Git), caricata automaticamente in ambiente Development |

### 12.6 Creazione e collegamento del progetto frontend (`TaskManager.Client`)

| Comando | Effetto |
|---|---|
| `dotnet new blazorwasm -o TaskManager.Client -n TaskManager.Client` | Genera un nuovo progetto Blazor WebAssembly standalone nella cartella `TaskManager.Client` |
| `dotnet sln TaskManager.Api.slnx add TaskManager.Client/TaskManager.Client.csproj` | Aggiunge il nuovo progetto alla solution esistente, così Visual Studio lo mostra insieme al backend |

### 12.7 Pacchetti NuGet installati — frontend (`TaskManager.Client`)

| Comando | Effetto |
|---|---|
| `dotnet add package Microsoft.AspNetCore.Components.Authorization` | Fornisce `AuthenticationStateProvider`, `<AuthorizeView>`, `CascadingAuthenticationState`, `AuthorizeRouteView` — l'infrastruttura di autenticazione/autorizzazione lato componenti Blazor |
| `dotnet add package Blazored.LocalStorage` | Espone il `localStorage` del browser come servizio .NET (`ILocalStorageService`), usato per salvare il token JWT tra una visita e l'altra |
| `dotnet add package Microsoft.Extensions.Http` | Fornisce `AddHttpClient`/`IHttpClientFactory`, per registrare un `HttpClient` "con nome" con un `DelegatingHandler` collegato |

### 12.8 Avvio dell'applicazione

| Comando | Effetto |
|---|---|
| `dotnet run` | Compila e avvia l'applicazione da terminale. Non apre automaticamente il browser (a differenza di F5 in Visual Studio) e può usare solo il profilo HTTP di default, non HTTPS |
| `dotnet watch run` | Come `dotnet run`, ma ricompila e riavvia automaticamente il progetto ogni volta che un file viene salvato — comodo durante lo sviluppo attivo, evita di fermare/riavviare manualmente ad ogni modifica |

> Con due progetti (backend + frontend) va eseguito separatamente in due terminali — vedi sezione 10.13.

### 12.9 Compilazione

| Scorciatoia | Effetto |
|---|---|
| `Ctrl+Shift+B` (Visual Studio, non un comando bash) | Compila l'intera solution (entrambi i progetti), utile per verificare rapidamente la presenza di errori senza dover avviare l'applicazione |

---

## Glossario rapido dei termini e acronimi incontrati

| Sigla | Significato esteso | Descrizione |
|---|---|---|
| **API** | Application Programming Interface | Insieme di "porte d'accesso" che un programma espone verso l'esterno |
| **REST** | REpresentational State Transfer | Stile architetturale per progettare API basate su risorse, URL e verbi HTTP |
| **CRUD** | Create, Read, Update, Delete | Le quattro operazioni base su una risorsa/entità |
| **HTTP** | HyperText Transfer Protocol | Protocollo di comunicazione usato per le richieste/risposte web |
| **HTTPS** | HTTP Secure | Versione cifrata/sicura del protocollo HTTP |
| **URL** | Uniform Resource Locator | Indirizzo che identifica una risorsa (es. un endpoint di un'API) |
| **JSON** | JavaScript Object Notation | Formato testuale leggero per rappresentare dati strutturati, usato per il corpo di richieste/risposte delle API |
| **DTO** | Data Transfer Object | Classe separata dal modello di dominio, usata per controllare esattamente quali dati entrano/escono da un'API |
| **ORM** | Object-Relational Mapping | Tecnica/strumento (Entity Framework Core, nel nostro caso) che traduce oggetti del linguaggio in righe di un database relazionale, e viceversa |
| **EF Core** | Entity Framework Core | L'ORM ufficiale Microsoft per .NET, usato in questo progetto per parlare col database |
| **SQL** | Structured Query Language | Linguaggio standard per interrogare e manipolare database relazionali |
| **FK** | Foreign Key (chiave esterna) | Colonna che fa riferimento alla chiave primaria di un'altra tabella |
| **DI** | Dependency Injection | Pattern per cui le dipendenze di una classe vengono "iniettate" dall'esterno invece che create al suo interno |
| **LINQ** | Language Integrated Query | Sintassi unificata, integrata nel linguaggio C#, per scrivere query su collezioni e database |
| **JWT** | JSON Web Token | Token firmato digitalmente, usato per autenticare le richieste senza che il server debba mantenere stato |
| **DDD** | Domain-Driven Design | Approccio alla progettazione del software centrato sulla modellazione del dominio di business |
| **MVC** | Model-View-Controller | Pattern architetturale che separa dati (Model), interfaccia (View) e logica di controllo (Controller) |
| **IDE** | Integrated Development Environment | Ambiente di sviluppo integrato — es. Visual Studio |
| **NuGet** | — (nome proprio, non un acronimo) | Gestore di pacchetti/librerie ufficiale per l'ecosistema .NET |
| **SPA** | Single Page Application | Applicazione web che carica una sola pagina HTML e poi aggiorna il contenuto dinamicamente (via JavaScript/WebAssembly), senza ricaricare l'intera pagina a ogni navigazione |
| **WASM** | WebAssembly | Formato binario eseguibile direttamente dal browser a velocità prossima al codice nativo; è ciò che permette a Blazor di eseguire codice C# (compilato in WASM) interamente lato client |
| **CORS** | Cross-Origin Resource Sharing | Meccanismo con cui un server dichiara esplicitamente quali altre origini (domini/porte) sono autorizzate a chiamarlo da codice eseguito nel browser |

---

## Glossario rapido dei termini tecnici (non acronimi)

| Termine | Descrizione |
|---|---|
| `DbContext` | Classe che rappresenta la connessione/sessione verso il database, traduce le classi C# in tabelle |
| `DbSet<T>` | Rappresenta una tabella, punto di accesso per le query |
| Migration | File generato che descrive le modifiche allo schema del database, basate sulle classi modello |
| Navigation Property | Proprietà C# che permette di "navigare" verso l'oggetto collegato, senza query manuali — non è una colonna reale |
| Shadow Property | Foreign Key creata automaticamente da EF Core quando non dichiarata esplicitamente nel codice |
| Lifetime (DI) | Durata di vita di un'istanza di un servizio: Transient, Scoped, Singleton |
| `async` / `await` | Parole chiave per scrivere codice asincrono, non bloccante |
| `Task` / `Task<T>` | Rappresentano un'operazione asincrona in corso, con o senza un valore di ritorno |
| `PasswordHasher<T>` | Classe di ASP.NET Core Identity per hashare/verificare le password in modo sicuro |
| `ActionResult<T>` | Tipo di ritorno che permette a un metodo di un controller di restituire sia un oggetto `T` sia una risposta HTTP esplicita (`NotFound`, `Ok`, ecc.) |
| `ClaimsPrincipal` / `Claim` | Rappresentazione .NET standard di "un utente autenticato": un insieme di `Claim` (coppie chiave-valore come Id, nome, ruolo) che descrivono chi è |
| `AuthenticationStateProvider` | Classe Blazor che risponde alla domanda "chi è l'utente attuale?"; nei progetti standalone va implementata a mano (qui: `ApiAuthenticationStateProvider`) |
| `DelegatingHandler` | Classe che si inserisce nel percorso di ogni richiesta di un `HttpClient`, potendola leggere o modificare prima che parta (qui: per aggiungere l'header `Authorization`) |
| `localStorage` | Spazio di archiviazione del browser, specifico per sito, che sopravvive alla chiusura della pagina — usato per conservare il token JWT tra una visita e l'altra |
| `<AuthorizeView>` | Componente Blazor che mostra contenuti diversi nella stessa pagina in base al login, senza bloccare l'intera pagina (a differenza di `[Authorize]`) |
| `CascadingAuthenticationState` | Componente che rende lo stato di autenticazione disponibile a tutti i componenti figli, a qualsiasi profondità, senza passarlo manualmente |
| JS Interop | Meccanismo con cui codice C# in Blazor WebAssembly chiama funzioni JavaScript del browser (es. l'accesso a `localStorage`), e viceversa |

---

*Fine documento.*
