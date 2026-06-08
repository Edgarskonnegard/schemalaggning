# Schemaläggning

Schemaläggning är ett fullstack-projekt för att hantera butiksschema. Projektet innehåller adminflöden för att skapa anställda, roller, passtyper, bemanningsbehov, grundscheman och aktiva scheman. Det finns också employee-flöden för att se sitt schema, ansöka om ledighet, kommentera pass och hantera passbyten.

Projektet är byggt som ett examensprojekt och fokuserar på schemagenerering, rollbaserad access och testning av kritiska backend-flöden.

## Tech Stack

- ASP.NET Core Web API
- Entity Framework Core
- SQL Server
- React
- Vite
- React Router
- JWT authentication
- xUnit
- WebApplicationFactory
- SQLite in-memory för integration tests
- GitHub Actions för CI

## Funktioner

- Admin-login med JWT.
- Hantering av employees, roles och shift types.
- Koppling mellan roles och tillåtna shift types.
- Butiker och bemanningsbehov per butik.
- Generering av grundschema utifrån bemanningsbehov och regler.
- Godkännande av genererade grundscheman.
- Generering och publicering av aktiva scheman.
- Employee-dashboard med kommande pass.
- Leave requests med admin approval.
- Shift comments som notifierar admin.
- Shift swap requests mellan anställda.
- Route guards och rollbaserad frontend-navigation.
- Backend authorization för admin- och employee-flöden.

## Projektstruktur

```text
backend/          ASP.NET Core Web API, EF Core, controllers, services, repositories
backend.Tests/    xUnit-tester, integration tests och service-nära tester
frontend/         React/Vite frontend
backend/docs/     Projektanteckningar, todo och lokal utredning
.github/          GitHub Actions workflow
```

## Kom igång

### Förutsättningar

- .NET 9 SDK
- Node.js och npm
- SQL Server lokalt eller via Docker

Backend använder som standard connection string i `backend/appsettings.json`:

```json
"DefaultConnection": "Server=localhost;Database=SchemalaggningDb;User Id=sa;Password=YourPassword123;Trust Server Certificate=True;"
```

Ändra connection string vid behov i `backend/appsettings.Development.json` eller via user secrets.

## Backend

Återställ dependencies:

```bash
dotnet restore schemalaggning.sln
```

Kör databas-migrationer:

```bash
dotnet ef database update --project backend
```

Starta backend:

```bash
dotnet run --project backend
```

Backend körs normalt på:

```text
http://localhost:5224
```

## Frontend

Installera dependencies:

```bash
cd frontend
npm install
```

Starta frontend:

```bash
npm run dev
```

Frontend körs normalt på:

```text
http://localhost:5173
```

Vite proxyar API-anrop från `/api` till:

```text
http://localhost:5224
```

## Login

I development seedas ett admin-konto automatiskt om det saknas:

```text
username: admin
password: admin
```

Employee-konton skapas via adminflödet och kopplas till en employee.

## Tester

Kör alla backend-tester:

```bash
dotnet test schemalaggning.sln
```

Testsviten innehåller:

- endpoint tests för auth-känsliga flöden
- integration tests med WebApplicationFactory
- SQLite in-memory som testdatabas
- service-nära tester för schemagenerering och schemaregler
- provider-experiment mellan EF Core InMemory och SQLite in-memory

Exempel på testade flöden:

- employee får kommentera egna publicerade pass men inte andras
- admin kan hantera passkommentarer
- employee kan skapa leave request och admin kan godkänna
- schema kan inte publiceras om perioden överlappar ett publicerat schema
- shift swap flyttar passet när mottagaren godkänner
- schemagenerering fortsätter från rätt datum och tar hänsyn till ledighet

## CI

Projektet har en GitHub Actions pipeline som kör:

```text
dotnet restore
dotnet build
dotnet test
npm ci
npm run build
```

Tanken är att build och tester ska vara gröna innan kod mergas.

## Teststrategi

Projektet använder framför allt integration tests för kritiska backend workflows. Anledningen är att flera viktiga regler beror på flera lager samtidigt: HTTP endpoints, JWT authentication, authorization, Entity Framework Core och database state.

För testdatabas valdes SQLite in-memory som första steg. Det ger mer realistiskt databeteende än EF Core InMemory, men är enklare att köra lokalt och i CI än en riktig SQL Server eller Testcontainers. SQL Server/Testcontainers hade varit ett rimligt nästa steg i ett större arbetsplatsprojekt där maximal produktionslikhet är viktigare.

## Kvar att göra

- Fortsätta förbättra schemagenereringsalgoritmen.
- Utöka regler för dygnsvila, helgfördelning och sysselsättningsgrad.
- Förbättra user-flöden för passbyten och ledighet.
- Utvärdera frontend component tests eller smoke tests.
- Städa bort build artifacts från git om `bin/` och `obj/` fortfarande är trackade.
- Förbereda tydligare staging/production-konfiguration.
