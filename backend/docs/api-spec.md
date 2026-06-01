# API Spec

Det här dokumentet beskriver planerade API-endpoints för backendens V1. Syftet är att vara ett kontrakt för framtida implementation, inte en garanti för att allt redan finns i kod.

## Bas

- Basväg: `/api`
- Format: JSON
- Datumformat: ISO 8601, exempel `2026-06-01`
- Tidformat: ISO 8601 time, exempel `08:00:00`
- Rekommenderat felformat: `ProblemDetails`

## V1-omfattning

API:t ska i första versionen stödja:

- Employees
- ShiftTypes
- BaseScheduleRules
- Schedule generation
- Schedule publishing

Eftersom backendmodellen använder `RoleId` finns även enkla Role-endpoints i implementationen.

## Roles

### GET `/api/roles`

Returnerar alla roller.

### GET `/api/roles/{id}`

Returnerar en roll.

### POST `/api/roles`

Skapar en roll.

```json
{
  "name": "Kassa"
}
```

### PUT `/api/roles/{id}`

Uppdaterar en roll.

### DELETE `/api/roles/{id}`

Tar bort en roll.

API:t ska inte i V1 stödja:

- JWT
- SignalR
- passbyten
- sjukanmälan
- preferenser
- tillgänglighet
- ML-baserad schemagenerering
- avancerad optimering

## Employees

### GET `/api/employees`

Returnerar alla anställda.

### GET `/api/employees/{id}`

Returnerar en anställd.

### GET `/api/employees/{id}/details`

Returnerar en anställd med roll och grundschema.

### POST `/api/employees`

Skapar en anställd.

Exempel:

```json
{
  "name": "Anna Andersson",
  "roleId": 1,
  "employmentPercentage": 100
}
```

### PUT `/api/employees/{id}`

Uppdaterar en anställd.

### DELETE `/api/employees/{id}`

Tar bort en anställd.

Det behöver beslutas hur historiska scheman och pass ska påverkas.

## ShiftTypes

### GET `/api/shift-types`

Returnerar alla passtyper.

### GET `/api/shift-types/{id}`

Returnerar en passtyp.

### POST `/api/shift-types`

Skapar en passtyp.

Exempel:

```json
{
  "name": "Öppning",
  "roleId": 1,
  "defaultStartTime": "08:00:00",
  "defaultEndTime": "16:00:00"
}
```

### PUT `/api/shift-types/{id}`

Uppdaterar en passtyp.

Viktigt: ändringar av passtypens standardtider får inte automatiskt ändra redan genererade pass.

### DELETE `/api/shift-types/{id}`

Tar bort en passtyp.

Det bör blockeras om passtypen används av grundschemaregler eller historiska pass, om inte en tydlig arkiveringsstrategi införs.

## Rollbaserade passtyper

En anställd får arbeta passtyper som hör till samma roll som den anställda:

```text
Employee.RoleId == ShiftType.RoleId
```

Om en anställd ska få andra passtyper ändras den anställdas roll, alternativt skapas en bredare roll.

## Base Schedule

Grundschema hanteras per anställd.

### GET `/api/employees/{employeeId}/base-schedule`

Returnerar den anställdas grundschema.

Exempel på svar:

```json
[
  {
    "weekInCycle": 1,
    "dayOfWeek": "Monday",
    "shiftTypeId": 1,
    "shiftTypeName": "Öppning"
  }
]
```

### PUT `/api/employees/{employeeId}/base-schedule`

Sätter eller ersätter grundschemaregel för en viss vecka i fyraveckorscykeln och veckodag.

Exempel:

```json
{
  "weekInCycle": 1,
  "dayOfWeek": "Monday",
  "shiftTypeId": 1
}
```

Regeln betyder: sätt eller ersätt den anställdas regel för måndag i cykelvecka 1.

Varje regel måste referera till en passtyp som matchar den anställdas roll.

### DELETE `/api/employees/{employeeId}/base-schedule/{weekInCycle}/{dayOfWeek}`

Tar bort grundschemaregeln för en viss cykelvecka och veckodag.

## Schedules

### GET `/api/schedules`

Returnerar scheman.

### GET `/api/schedules/{id}`

Returnerar ett schema med dess pass.

### POST `/api/schedules/generate`

Genererar ett nytt schema från grundscheman.

Exempel:

```json
{
  "name": "Juni 2026",
  "periodStart": "2026-06-01",
  "periodEnd": "2026-06-30"
}
```

Förväntat beteende:

- skapar ett nytt schema med status `Draft`
- skapar faktiska pass från `BaseScheduleRule` genom att matcha fyraveckorscykel och veckodag
- kopierar tider från `ShiftType` till varje `Shift`
- ändrar inte `ShiftType`
- ändrar inte `BaseScheduleRule`

Exempel på svar:

```json
{
  "id": 1,
  "name": "Juni 2026",
  "periodStart": "2026-06-01",
  "periodEnd": "2026-06-30",
  "status": "Draft",
  "createdShifts": 42
}
```

### PUT `/api/schedules/{id}/publish`

Publicerar ett schema.

Förväntat beteende:

- endast `Draft`-scheman kan publiceras
- status ändras från `Draft` till `Published`
- schemat blir verksamhetens aktiva schema
- passen i schemat kan också få status `Published`

## Shifts

Manuell redigering av genererade pass ingår efter att schedule generation finns.

### PUT `/api/shifts/{shiftId}`

Uppdaterar ett faktiskt pass.

Viktigt: uppdateringen får endast ändra passet. Den får inte ändra passtypen eller grundschemat.

Exempel:

```json
{
  "employeeId": 1,
  "shiftTypeId": 1,
  "date": "2026-06-15",
  "startTime": "09:00:00",
  "endTime": "16:00:00"
}
```

Vid byte av anställd eller passtyp måste systemet validera att den anställda får arbeta passtypen.

## Vanliga statuskoder

| Kod | Användning |
| --- | --- |
| 200 | Läsning, uppdatering eller publicering lyckades |
| 201 | Resurs skapades |
| 204 | Borttagning lyckades |
| 400 | Ogiltig request |
| 404 | Resurs saknas |
| 409 | Konflikt med affärsregel |

## Implementerat idag

Nuvarande backend kan fortfarande innehålla scaffoldad `/weatherforecast`. Den hör inte till domänen och ska tas bort när riktiga V1-endpoints implementeras.
