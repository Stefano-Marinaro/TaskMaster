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
- Web API restituisce SOLO dati (JSON), pensata per essere "consumata" da qualcos'altro: un'app React, mobile, o un frontend separato (nel nostro caso, in futuro, un frontend Blazor)

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

## 9. Autenticazione JWT (in corso)

**Punto in cui si è arrivati**: hashing delle password completato, endpoint di registrazione in fase di scrittura. Login e generazione/validazione dei token JWT ancora da fare.

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

### 9.5 `RegisterDto` (in corso)

- Cartella: `Dtos/`
- File: `RegisterDto.cs`

```csharp
public class RegisterDto
{
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;  // password in chiaro, MAI PasswordHash
}
```

Motivo: il client non deve mai calcolare l'hash da solo — manda la password in chiaro (su connessione HTTPS), e il server la hasha prima di salvarla.

### 9.6 `AuthController` (in corso)

- Cartella: `Controllers/`
- File: `AuthController.cs`
- Route: `api/auth`

Richiede DUE servizi nel costruttore (primo esempio di DI con più dipendenze contemporaneamente):

```csharp
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly PasswordService _passwordService;

    public AuthController(AppDbContext context, PasswordService passwordService)
    {
        _context = context;
        _passwordService = passwordService;
    }

    // metodo Register in corso di scrittura:
    // 1. Riceve un RegisterDto dal body
    // 2. Crea un nuovo User copiando UserName/Email dal DTO
    // 3. Calcola PasswordHash con _passwordService.HashPassword(user, dto.Password)
    // 4. Salva con AddAsync + SaveChangesAsync
    // 5. Restituisce una risposta (da definire: User completo? UserDto? conferma?)
}
```

### 9.7 Prossimi passi da affrontare

- Completare il metodo `Register`
- Endpoint `Login` (verifica credenziali + generazione del token JWT)
- Configurazione della validazione JWT in `Program.cs` (chiave segreta, parametri di validazione)
- Attributo `[Authorize]` per proteggere gli endpoint esistenti
- Differenziazione dei permessi in base al `Role` (Owner vs Member)

---

## 10. Piano futuro (non ancora affrontato)

- Completamento autenticazione JWT (vedi sezione 9.7)
- Test con dati realmente collegati tra loro (Project con Task assegnati a User specifici) — verificare dal vivo il comportamento delle relazioni e il rischio di cicli di serializzazione JSON quando si useranno `Include()`
- Frontend in C# (Blazor) per una demo funzionante del progetto, da costruire DOPO aver completato e consolidato le API
- Possibili raffinamenti futuri:
  - `OnModelCreating` per configurazioni esplicite su relazioni più complesse (es. se la relazione molti-a-molti User-Project dovesse arricchirsi di dati propri, come il ruolo dell'utente in quello specifico progetto)
  - Endpoint dedicato per spostare un `TaskItem` tra `Project` diversi, separato dal PUT generico di aggiornamento (separazione delle responsabilità)
  - Filtri/ricerca sui Task, notifiche, statistiche/dashboard

---

## 11. Comandi da terminale utilizzati (in ordine cronologico)

Di seguito tutti i comandi bash/terminale incontrati durante lo sviluppo, nell'ordine in cui sono comparsi nel percorso.

### 11.1 Setup Git / GitHub

| Comando | Effetto |
|---|---|
| `dotnet new gitignore` | Genera un file `.gitignore` già pronto per progetti .NET (esclude `bin/`, `obj/`, `.vs/`), evitando di scriverlo a mano |
| `git init` | Inizializza un repository Git nella cartella corrente |
| `git add .` | Prepara (stage) tutti i file della cartella per il prossimo commit |
| `git commit -m "Initial commit: TaskManager.Api scaffold"` | Crea il primo commit con i file preparati |
| `git remote add origin <URL-repo>` | Collega il repository locale a quello remoto su GitHub |
| `git branch -M main` | Rinomina il branch principale in "main" |
| `git push -u origin main` | Invia (push) i commit locali al repository remoto su GitHub, e imposta "origin main" come destinazione predefinita per i push successivi |

### 11.2 Certificato HTTPS di sviluppo

| Comando | Effetto |
|---|---|
| `dotnet dev-certs https --trust` | Genera (se non esiste) e installa come affidabile il certificato HTTPS di sviluppo di ASP.NET Core, necessario per far funzionare HTTPS in locale su localhost senza avvisi del browser |

### 11.3 Pacchetti NuGet installati

| Comando | Effetto |
|---|---|
| `dotnet add package Swashbuckle.AspNetCore` | Installa Swagger UI classico (sostituendo il template OpenAPI di default di .NET 9+), per avere l'interfaccia grafica interattiva delle API in fase di sviluppo |
| `dotnet add package Microsoft.EntityFrameworkCore.SqlServer` | Installa il provider Entity Framework Core per SQL Server, necessario per collegarsi a un database SQL Server/LocalDB |
| `dotnet add package Microsoft.EntityFrameworkCore.Tools` | Installa gli strumenti da riga di comando di Entity Framework Core (necessari per i comandi `dotnet ef ...`) |
| `dotnet add package Microsoft.AspNetCore.Identity` | Installa le classi di ASP.NET Core Identity, usate per `PasswordHasher<T>` (hashing e verifica sicura delle password) |
| `dotnet tool install --global dotnet-ef` | (Solo se `dotnet ef` non viene riconosciuto) Installa globalmente lo strumento a riga di comando di Entity Framework Core |

### 11.4 Migrations e database (Entity Framework Core)

| Comando | Effetto |
|---|---|
| `dotnet ef migrations add InitialCreate` | Genera i file di Migration (in `Migrations/`) che descrivono le istruzioni per creare le tabelle nel database, basandosi sulle classi modello e su `AppDbContext`. Non tocca ancora il database |
| `dotnet ef migrations remove` | Rimuove l'ultima Migration generata (non ancora applicata al database), usato quando il modello cambia prima di aver eseguito `database update` |
| `dotnet ef database update` | Applica davvero le istruzioni della Migration al database, creando o modificando concretamente le tabelle |

### 11.5 Avvio dell'applicazione

| Comando | Effetto |
|---|---|
| `dotnet run` | Compila e avvia l'applicazione da terminale. Non apre automaticamente il browser (a differenza di F5 in Visual Studio) e può usare solo il profilo HTTP di default, non HTTPS |
| `dotnet watch run` | Come `dotnet run`, ma ricompila e riavvia automaticamente il progetto ogni volta che un file viene salvato — comodo durante lo sviluppo attivo, evita di fermare/riavviare manualmente ad ogni modifica |

### 11.6 Compilazione

| Scorciatoia | Effetto |
|---|---|
| `Ctrl+Shift+B` (Visual Studio, non un comando bash) | Compila l'intera solution, utile per verificare rapidamente la presenza di errori senza dover avviare l'applicazione |

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

---

*Fine documento.*
