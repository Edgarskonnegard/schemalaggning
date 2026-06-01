# Architecture

## Översikt

Projektet är ett fullstack-system med React-frontend, ASP.NET Core-backend och SQL Server-databas via Entity Framework Core.

Backend ska byggas med en tydlig lagerindelning:

```text
Controllers
Services
Repositories
Entity Framework
SQL Server
```

## Ansvar per lager

### Controllers

Controllers ansvarar endast för API.

De ska:

- ta emot HTTP-requests
- anropa rätt service
- returnera HTTP-responses
- mappa request/response DTO:er vid behov

De ska inte innehålla affärslogik.

### Services

Services ansvarar för affärslogik.

Exempel på serviceansvar:

- kontrollera att en anställd får arbeta en passtyp
- skapa och uppdatera grundschema
- generera schema från grundschemaregler
- kopiera passtypens tider till genererade pass
- publicera schema
- säkerställa att ändringar av pass inte ändrar passtyp eller grundschema

### Repositories

Repositories ansvarar för databasanrop.

De ska:

- läsa och skriva entiteter
- kapsla in Entity Framework-frågor
- hantera includes och filtrering nära datalagret

De ska inte fatta affärsbeslut.

### Entity Framework

Entity Framework Core används som ORM och mappar domänentiteter till SQL Server.

### SQL Server

SQL Server är systemets persistenta datalager.

## Viktigaste arkitekturprinciperna

### Passtyp = Mall

`ShiftType` är en mall för arbetspass. Den innehåller standardnamn och standardtider.

Exempel:

```text
Öppning
08:00 - 16:00
```

### Grundschema = Återkommande regler

`BaseScheduleRule` beskriver vad en anställd normalt arbetar under en viss vecka i en fyraveckorscykel och på en viss veckodag.

Exempel:

```text
Anna arbetar Öppning vecka 1 på måndagar.
```

### Genererat schema = Faktiska pass

När ett schema genereras skapas `Shift`-rader för faktiska datum.

Exempel:

```text
2026-06-15
Anna
08:00 - 16:00
```

Vid generering ska passtypens tider kopieras in i det faktiska passet.

Om chefen senare ändrar passet till:

```text
09:00 - 16:00
```

ska endast det passet ändras.

Passtypen får inte ändras. Grundschemat får inte ändras.

## Backendstruktur

Rekommenderad struktur:

```text
backend/
├── Controllers/
│   ├── EmployeesController.cs
│   ├── ShiftTypesController.cs
│   ├── BaseScheduleRulesController.cs
│   └── SchedulesController.cs
├── Data/
├── DTOs/
├── Models/
├── Repositories/
│   ├── Interfaces/
│   ├── EmployeeRepository.cs
│   ├── ShiftTypeRepository.cs
│   ├── BaseScheduleRuleRepository.cs
│   └── ScheduleRepository.cs
├── Services/
│   ├── Interfaces/
│   ├── EmployeeService.cs
│   ├── ShiftTypeService.cs
│   ├── BaseScheduleRuleService.cs
│   ├── ScheduleService.cs
│   └── ScheduleGenerationService.cs
└── Program.cs
```

Nuvarande kodbas har modeller, DTO:er och `AppDbContext`. Controllers, repositories och services ska införas när API och affärslogik implementeras.

Se även `docs/domain-model.md` för UML, relationer och entiteternas ansvar.

## Entiteter

### Employee

- `Id`
- `Name`
- `RoleId`
- `EmploymentPercentage`

### Role

- `Id`
- `Name`

### ShiftType

- `Id`
- `Name`
- `RoleId`
- `DefaultStartTime`
- `DefaultEndTime`

### BaseScheduleRule

- `Id`
- `EmployeeId`
- `ShiftTypeId`
- `WeekInCycle`
- `DayOfWeek`

### Schedule

- `Id`
- `Name`
- `PeriodStart`
- `PeriodEnd`
- `Status`

### Shift

- `Id`
- `ScheduleId`
- `EmployeeId`
- `ShiftTypeId`
- `Date`
- `StartTime`
- `EndTime`
- `Status`

## Statushantering

Scheman skapas alltid som `Draft`.

När ett schema publiceras ändras status från `Draft` till `Published`.

I V1 räcker dessa statusvärden för schema:

- `Draft`
- `Published`

Pass kan använda status för att skilja planerade, ändrade eller borttagna pass senare, men avancerad statushantering ingår inte i V1 om den inte behövs för publicering.

## Implementationsnoteringar

- Controllers ska vara tunna.
- Services ska vara platsen för domänregler.
- Repositories ska vara platsen för EF Core-frågor.
- DTO:er ska användas som API-kontrakt.
- Entiteter ska inte exponeras direkt om det riskerar att läcka intern struktur.
- Dokumentationen ska uppdateras när domänmodellen eller API-kontraktet ändras.

## Repository-kontrakt

Repository-metoderna nedan är rekommenderade för V1. De är ett riktmärke för implementationen, inte ett krav på exakt filinnehåll om en bättre lokal struktur växer fram.

### EmployeeRepository

Ansvar:

- hämta alla anställda
- hämta en anställd med roll och grundschema
- skapa, uppdatera och ta bort anställd
- kontrollera om en anställds roll matchar en passtyp

Föreslagna metoder:

```csharp
Task<List<Employee>> GetAllAsync();
Task<Employee?> GetByIdAsync(int id);
Task<Employee?> GetByIdWithDetailsAsync(int id);
Task<Employee> CreateAsync(Employee employee);
Task<bool> UpdateAsync(Employee employee);
Task<bool> DeleteAsync(int id);
Task<bool> CanWorkShiftTypeAsync(int employeeId, int shiftTypeId);
```

### ShiftTypeRepository

Ansvar:

- CRUD för passtyper
- hämta passtyper efter roll

Föreslagna metoder:

```csharp
Task<List<ShiftType>> GetAllAsync();
Task<ShiftType?> GetByIdAsync(int id);
Task<List<ShiftType>> GetByRoleAsync(string role);
Task<ShiftType> CreateAsync(ShiftType shiftType);
Task<bool> UpdateAsync(ShiftType shiftType);
Task<bool> DeleteAsync(int id);
```

### BaseScheduleRuleRepository

Ansvar:

- CRUD för grundschemaregler
- hämta grundschema för en anställd
- sätta eller ersätta grundregel för viss cykelvecka och dag

Föreslagna metoder:

```csharp
Task<List<BaseScheduleRule>> GetByEmployeeIdAsync(int employeeId);
Task<BaseScheduleRule?> GetByEmployeeWeekAndDayAsync(int employeeId, int weekInCycle, DayOfWeek dayOfWeek);
Task<BaseScheduleRule> CreateAsync(BaseScheduleRule rule);
Task<bool> UpdateAsync(BaseScheduleRule rule);
Task<bool> DeleteAsync(int id);
Task<bool> DeleteByEmployeeWeekAndDayAsync(int employeeId, int weekInCycle, DayOfWeek dayOfWeek);
```

### ScheduleRepository

Ansvar:

- skapa schema
- hämta schema med pass
- uppdatera schemastatus
- uppdatera enskilda pass

Föreslagna metoder:

```csharp
Task<List<Schedule>> GetAllAsync();
Task<Schedule?> GetByIdAsync(int id);
Task<Schedule?> GetByIdWithShiftsAsync(int id);
Task<Schedule> CreateAsync(Schedule schedule);
Task<bool> UpdateAsync(Schedule schedule);
Task<bool> DeleteAsync(int id);
Task<Shift?> GetShiftByIdAsync(int shiftId);
Task<bool> UpdateShiftAsync(Shift shift);
```

## Service-kontrakt

### EmployeeService

Ansvar:

- validera input
- skapa och uppdatera anställda
- säkerställa att rollen finns

Föreslagna metoder:

```csharp
Task<List<EmployeeReadDto>> GetAllAsync();
Task<EmployeeReadDto?> GetByIdAsync(int id);
Task<EmployeeReadDto?> GetDetailsAsync(int id);
Task<EmployeeReadDto> CreateAsync(EmployeeCreateDto dto);
Task<bool> UpdateAsync(int id, EmployeeUpdateDto dto);
Task<bool> DeleteAsync(int id);
```

### ShiftTypeService

Ansvar:

- skapa och uppdatera passtyper
- säkerställa att sluttid är efter starttid
- hantera roll och standardtider

Föreslagna metoder:

```csharp
Task<List<ShiftTypeReadDto>> GetAllAsync();
Task<ShiftTypeReadDto?> GetByIdAsync(int id);
Task<ShiftTypeReadDto> CreateAsync(ShiftTypeCreateDto dto);
Task<bool> UpdateAsync(int id, ShiftTypeUpdateDto dto);
Task<bool> DeleteAsync(int id);
```

### BaseScheduleRuleService

Ansvar:

- skapa och uppdatera grundschema för anställd
- kontrollera att anställdas roll matchar vald passtyp
- kontrollera att det bara finns en regel per anställd, cykelvecka och dag
- ta bort grundpass för viss cykelvecka och dag

Föreslagna metoder:

```csharp
Task<List<BaseScheduleRuleReadDto>> GetByEmployeeIdAsync(int employeeId);
Task<BaseScheduleRuleReadDto> SetRuleAsync(int employeeId, BaseScheduleRuleCreateDto dto);
Task<bool> DeleteRuleAsync(int employeeId, int weekInCycle, DayOfWeek dayOfWeek);
```

### ScheduleGenerationService

Ansvar:

- generera faktiska pass från grundschema
- skapa ett schema för en period
- kopiera tider från `ShiftType` till `Shift`
- inte påverka grundschemat efter generering

Föreslagen metod:

```csharp
Task<ScheduleReadDto> GenerateFromBaseScheduleAsync(ScheduleCreateDto dto);
```

### ScheduleService

Ansvar:

- hämta scheman
- redigera enskilda pass
- publicera schema

Föreslagna metoder:

```csharp
Task<List<ScheduleReadDto>> GetAllAsync();
Task<ScheduleReadDto?> GetByIdAsync(int id);
Task<bool> UpdateShiftAsync(int shiftId, ShiftUpdateDto dto);
Task<bool> PublishScheduleAsync(int scheduleId);
```

## Implementationsordning för V1

1. Säkerställ att modeller och `AppDbContext` matchar domänmodellen.
2. Skapa migration och uppdatera databas.
3. Implementera Employees och ShiftTypes först.
4. Implementera BaseScheduleRule.
5. Implementera ScheduleGenerationService.
6. Implementera publicering av schema.
7. Implementera manuell redigering av genererade pass.
8. Ta bort scaffoldad `/weatherforecast`.
