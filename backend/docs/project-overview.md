# Project Overview

## Syfte

Det här projektet är ett webbaserat schemaläggningssystem för små verksamheter, till exempel butiker, cafeer och restauranger.

Systemet ska hjälpa en chef att bygga ett grundschema, generera faktiska scheman för valda perioder, göra manuella justeringar och publicera schemat som verksamhetens aktiva schema.

Dokumenten i `docs/` är projektets centrala kunskapskälla. Framtida implementationer ska utgå från dessa affärsregler, arkitekturprinciper och API-beskrivningar. När ny funktionalitet introduceras ska dokumentationen uppdateras samtidigt.

## Huvudflöde för chef

1. Skapa anställda.
2. Skapa passtyper.
3. Ange vilka passtyper en anställd får arbeta.
4. Skapa grundschema per anställd.
5. Generera ett schema för en vald period baserat på grundscheman.
6. Redigera genererade pass manuellt.
7. Publicera schemat.

När ett schema publiceras blir det verksamhetens aktiva schema.

## Teknik

| Del | Teknik |
| --- | --- |
| Frontend | React |
| Backend | ASP.NET Core |
| Databas | SQL Server |
| ORM | Entity Framework Core |

## Centrala domänobjekt

### Employee

En anställd i verksamheten.

### ShiftType

En passtyp eller mall. Exempel:

- Öppning
- Stängning
- Kassa
- Lager

En passtyp innehåller standardtider som kopieras in i faktiska pass när ett schema genereras.

### EmployeeShiftType

Koppling mellan `Employee` och `ShiftType`.

Anger vilka passtyper en anställd får arbeta.

### BaseScheduleRule

En återkommande regel för en anställd.

Exempel:

- Anna arbetar Öppning varje måndag.

### Schedule

Ett schema för en specifik period.

Exempel:

- Juni 2026.

### Shift

Ett faktiskt arbetspass i ett schema.

Ett pass skapas från en passtyp vid schemagenerering, men ska därefter kunna ändras utan att passtypen eller grundschemat ändras.

## V1-omfattning

Första versionen ska endast stödja:

- Employees
- ShiftTypes
- EmployeeShiftTypes
- BaseScheduleRules
- Schedule generation
- Schedule publishing

## Nuvarande implementation

Det som redan finns i backend:

- ASP.NET Core-projekt
- Entity Framework Core och SQL Server-koppling
- `AppDbContext`
- modeller för centrala domänobjekt
- DTO-mappar och flera DTO-klasser
- OpenAPI i utvecklingsmiljö

Det som återstår för V1:

- controllers
- repositories
- services
- migrations
- riktiga API-endpoints
- schemagenerering från grundschema
- publicering av schema
- manuell redigering av genererade pass
- borttagning av scaffoldad `/weatherforecast`

## Modellbeslut

Nuvarande implementation använder en normaliserad rollmodell:

- `Role` är en egen entitet.
- `Employee` refererar roll via `RoleId`.
- `ShiftType` refererar roll via `RoleId`.
- `Employee` använder `EmploymentPercentage`.

Det skiljer sig från den enklare tidiga beskrivningen där `Role` och `EmploymentType` var textfält. Framtida implementationer ska utgå från den normaliserade modellen om inget annat beslutas.

## Inte i V1

Följande ska inte implementeras ännu:

- JWT
- SignalR
- passbyten
- sjukanmälan
- preferenser
- tillgänglighet
- ML-baserad schemagenerering
- avancerad optimering

## Viktig princip

V1 ska vara ett enkelt, begripligt schemaläggningssystem med tydliga regler.

Passtypen är en mall. Grundschemat är återkommande regler. Det genererade schemat består av faktiska pass.

När chefen redigerar ett genererat pass ska endast det passet ändras.

## Dokumentationskarta

- `docs/domain-model.md` beskriver entiteter, relationer och UML/Mermaid-diagram.
- `docs/architecture.md` beskriver lager, ansvar och implementationens riktning.
- `docs/business-rules.md` beskriver affärsregler och invariants.
- `docs/api-spec.md` beskriver planerade V1-endpoints.
- `docs/todo.md` samlar kommande arbete och rekommenderad genomförandeordning.
