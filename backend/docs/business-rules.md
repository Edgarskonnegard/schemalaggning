# Business Rules

Det här dokumentet är projektets huvudsakliga källa för affärsregler. Framtida implementationer ska föreslås och byggas utifrån dessa regler.

## Kärnprinciper

### Passtyp är mall

`ShiftType` beskriver en typ av pass och dess standardtider.

Exempel:

```text
Öppning
08:00 - 16:00
```

### Grundschema är återkommande regler

`BaseScheduleRule` beskriver vad en anställd normalt arbetar på en viss veckodag.

Exempel:

```text
Anna arbetar Öppning varje måndag.
```

### Genererat schema är faktiska pass

`Schedule` innehåller faktiska `Shift`-pass för en vald period.

När ett schema genereras kopieras passtypens tider till varje genererat pass.

## Obligatoriska affärsregler

1. En anställd får endast tilldelas passtyper som finns i `EmployeeShiftType`.

2. En grundschemaregel måste referera till en passtyp som den anställda får arbeta.

3. Ett genererat pass är en kopia av passtypen vid genereringstillfället.

4. Ändringar av genererade pass får aldrig ändra passtypen.

5. Ändringar av genererade pass får aldrig ändra grundschemat.

6. Scheman skapas alltid som `Draft`.

7. Scheman kan publiceras.

8. När ett schema publiceras ändras status från `Draft` till `Published`.

## Exempel på korrekt beteende

Passtyp:

```text
Öppning
08:00 - 16:00
```

Grundschema:

```text
Anna arbetar Öppning på måndagar.
```

Genererat pass:

```text
2026-06-15
Anna
08:00 - 16:00
```

Om chefen ändrar passet till:

```text
09:00 - 16:00
```

ska endast det faktiska passet för `2026-06-15` ändras.

Följande får inte ändras av denna åtgärd:

- `ShiftType.DefaultStartTime`
- `ShiftType.DefaultEndTime`
- `BaseScheduleRule`

## Regler för EmployeeShiftType

- `EmployeeShiftType` är behörighetslistan för vilka passtyper en anställd får arbeta.
- Ett pass får inte skapas för en anställd om passtypen saknas i `EmployeeShiftType`.
- En grundschemaregel får inte skapas om passtypen saknas i `EmployeeShiftType`.

## Regler för schemagenerering

Vid generering ska systemet:

1. Ta emot period, exempelvis `2026-06-01` till `2026-06-30`.
2. Skapa ett nytt schema med status `Draft`.
3. Läsa anställdas grundschemaregler.
4. Matcha varje datum i perioden mot regelns `DayOfWeek`.
5. Skapa faktiska `Shift`-pass.
6. Kopiera `ShiftType.DefaultStartTime` till `Shift.StartTime`.
7. Kopiera `ShiftType.DefaultEndTime` till `Shift.EndTime`.
8. Spara passen på schemat.

Genereringen ska inte ändra passtyper eller grundschemaregler.

## Regler för manuell redigering

När ett genererat pass redigeras får systemet ändra:

- `Shift.Date`
- `Shift.StartTime`
- `Shift.EndTime`
- `Shift.EmployeeId`, om den nya anställda får arbeta passtypen
- `Shift.ShiftTypeId`, om den anställda får arbeta den nya passtypen
- `Shift.Status`

När ett genererat pass redigeras får systemet inte ändra:

- `ShiftType`
- `BaseScheduleRule`
- andra pass som råkar komma från samma passtyp eller grundregel

## Regler för publicering

- Ett schema kan publiceras.
- Endast scheman med status `Draft` ska kunna publiceras.
- Vid publicering ändras schemats status till `Published`.
- Ett publicerat schema blir verksamhetens aktiva schema.

Om systemet senare stödjer flera verksamheter måste aktivt schema hanteras per verksamhet.

## V1-avgränsningar

V1 ska inte innehålla:

- JWT
- SignalR
- passbyten
- sjukanmälan
- preferenser
- tillgänglighet
- ML-baserad schemagenerering
- avancerad optimering

## Öppna beslut

Följande är inte fastslaget ännu:

- Om flera pass per anställd och dag ska tillåtas.
- Om överlappande pass ska blockeras redan i V1.
- Om pass över midnatt ska tillåtas.
- Om publicering ska låsa schemat för vidare redigering.
- Hur historik ska hanteras när en anställd tas bort.
