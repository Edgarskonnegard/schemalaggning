# TODO

Det här dokumentet samlar saker som ska göras senare och rekommenderad ordning för större arbetsflöden.

## Rekommenderad genomförandeordning

1. Rensa projektgrunden
   - Lägg till root `.gitignore` så `backend/bin`, `backend/obj`, `frontend/node_modules` och liknande inte hamnar i versionshantering.
   - Lägg till root `README.md` med kommandon för backend, frontend och databas.

2. Databas och migrations
   - Säkerställ connection string för lokal SQL Server.
   - Skapa första EF Core migrationen från nuvarande domänmodell.
   - Kör databasen lokalt och verifiera att tabeller, relationer, unika index och decimalformat skapas korrekt.
   - Seed-data kan övervägas för roller, passtyper och testanställda.

3. Backend V1 utan auth
   - Verifiera att befintliga controllers, services och repositories fungerar mot riktig databas.
   - Testa endpoints för roller, anställda, passtyper, grundschema och schemagenerering.
   - Lägg till fokuserade tester för affärsregler, särskilt schemagenerering och ändringar av genererade pass.

4. Koppla frontend till backend
   - Lägg till ett litet API-lager i frontend, till exempel `src/api/client.js`, `employeesApi.js`, `rolesApi.js`, `shiftTypesApi.js` och `schedulesApi.js`.
   - Använd Vite proxy i utveckling så frontend kan anropa relativa URL:er som `/api/employees`.
   - Byt ut hårdkodad frontend-state stegvis:
     - `GET /api/employees`
     - `GET /api/roles`
     - `POST /api/employees`
     - `GET /api/shift-types`
     - `GET /api/employees/{id}/details`
     - `GET /api/schedules`
     - `POST /api/schedules/generate`
   - Mappa frontendens rollval till `roleId` och anställning till `employmentPercentage`.

5. JWT, inloggning och access
   - Inför auth efter att grundflödet fungerar mot databas och API, men innan employee-flöden som ledighet, shift-byten och admin-godkännande blir skarpa.
   - Lägg till användar-/konto-modell separat från `Employee`.
   - Koppla employee-konton till `Employee`.
   - Koppla admin-konton till butik eller systemnivå beroende på vald behörighetsmodell.
   - Stöd minst två accessnivåer:
     - `Admin`: kan hantera anställda, roller, passtyper, grundschema, generera och publicera schema.
     - `Employee`: kan läsa sitt eget schema och eventuellt sin egen profil.
   - Lägg till login-endpoint som returnerar JWT access token.
   - Backend ska validera JWT med `AddAuthentication().AddJwtBearer(...)`.
   - Backend ska använda `UseAuthentication()` före `UseAuthorization()`.
   - Skydda controllers/actions med `[Authorize]`, roller eller policies.
   - Exempel:
     - Admin-only: skapa/ändra/ta bort anställda, roller, passtyper och scheman.
     - Employee/Admin: läsa publicerat schema.
     - Employee-only data ska filtreras på inloggad användares id, inte på id från klienten.
   - Frontend ska ha auth-state, login-sida, token-hantering och route guards.
   - Frontend-låsning är bara UX. Riktig accesskontroll måste alltid ligga i backend.

## Nästa större domänriktning

Den nuvarande modellen räcker för att hantera roller, anställda, passtyper och ett enkelt grundschema. Nästa större riktning är att göra systemet butik-/verksamhetsbaserat och skilja tydligare på behov, förslag, godkännande och faktiskt schema.

### Målbild

- En `Store` representerar en butik/verksamhet.
- Anställda kopplas till en butik.
- Roller och passtyper kan vara butiksspecifika eller globala. Detta behöver beslutas.
- Butiken har ett grundbehov: vilka pass som behöver täckas i en normalvecka som upprepas.
- En schemagenereringstjänst fördelar butikens behovspass till anställda utifrån regler.
- Fördelade pass blir först förslag.
- Admin granskar och godkänner förslag innan passen blir del av schemat.
- Det allmänna schemat skapas från godkända pass samt händelser.
- Ändringar i det allmänna schemat sparas på faktiska `Shift` och påverkar inte grundschema eller butikens grundbehov.

### Implementerat hittills

- `Store` finns och anställda kan kopplas till butik.
- `UserAccount` finns som separat konto från `Employee`, med första login/JWT-grunden.
- `StoreCoverageRule` finns för butikens grundbehov i en repeterande normalvecka.
- Frontend har en första vy för att lägga till, ändra och ta bort bemanningsbehov per butik.

### Nästa rekommenderade steg

- Kör migrationen `AddStoreCoverageRules` mot lokal databas.
- Börja på en första fördelningstjänst som läser butikens `StoreCoverageRule` och skapar förslag, inte faktiska pass direkt.
- Lägg därefter till en enkel admin-vy för att granska och godkänna förslagen.

### Föreslagen domänmodell att utreda

```text
Store
- Id
- Name

UserAccount
- Id
- Email
- PasswordHash
- AccessRole: Admin | Employee
- EmployeeId?
- StoreId?
- IsActive

AdminStoreAccess (vid behov)
- UserAccountId
- StoreId
- PermissionLevel
- IsOwner

Employee
- StoreId
- RoleId
- EmploymentPercentage

StoreCoverageRule / StoreBaseShiftNeed
- StoreId
- ShiftTypeId
- DayOfWeek
- RequiredCount
- StartTime
- EndTime
```

`StoreCoverageRule` beskriver butikens behov, inte en anställds arbetspass.

Exempel:

```text
Butiken behöver 2 mellanpass varje tisdag.
Butiken behöver 1 stängningspass varje fredag.
```

```text
AssignmentProposal
- StoreCoverageRuleId eller generated need reference
- EmployeeId
- ShiftTypeId
- Date
- StartTime
- EndTime
- Status: Proposed | Approved | Rejected
```

`AssignmentProposal` är resultatet av fördelningstjänsten innan admin godkänner.

```text
Schedule
- StoreId
- PeriodStart
- PeriodEnd
- Status

Shift
- ScheduleId
- EmployeeId
- ShiftTypeId
- Date
- StartTime
- EndTime
- Source: ApprovedProposal | ManualEdit | ShiftSwap | EventAdjustment
```

`Shift` är det faktiska schemat. Manuell redigering, byten och händelser ska ändra `Shift`, inte grundschema eller butikens behovsregler.

```text
ScheduleEvent
- StoreId
- EmployeeId?
- Type: LeaveApproved | SickLeave | ManualBlock | Other
- PeriodStart
- PeriodEnd
- Status
```

`ScheduleEvent` behöver utredas mer senare. Exempel är godkänd ledighet, sjukdom, blockeringar eller andra händelser som påverkar schemat.

### Konton, inloggning och access

Konton bör införas som en egen modell, inte bakas in direkt i `Employee`.

Rekommenderad riktning:

- `UserAccount` är inloggningskontot.
- `Employee` är domänobjektet för anställning.
- Ett employee-konto kopplas till exakt en `Employee`.
- Ett admin-konto kopplas till en butik eller flera butiker beroende på framtida behov.
- JWT används för API-auth.
- Token ska innehålla användar-id, accessroll och relevanta claims, exempelvis `employeeId` och/eller `storeId`.
- Backend ska alltid filtrera data efter claims, inte lita på id:n som skickas från frontend.

Exempel på access:

```text
Admin
- Skapa och ändra butik.
- Skapa och ändra anställda.
- Skapa och ändra butikens grundbehov.
- Köra fördelningstjänst.
- Godkänna eller avvisa förslag.
- Publicera och redigera schema.

Employee
- Läsa sitt eget schema.
- Se sina egna pass.
- Ansöka om ledighet.
- Föreslå shift-byte.
- Se status på egna ansökningar och byten.
```

Backend-regler:

- Admin-endpoints skyddas med `[Authorize]` och policy/rollkrav.
- Employee-endpoints ska baseras på inloggad användares `EmployeeId`.
- En employee ska inte kunna läsa eller ändra någon annans privata data genom att byta id i URL:en.
- Store-data ska filtreras på adminens tillåtna butik/butiker.
- Frontend-route guards är bara UX. All riktig accesskontroll måste ligga i backend.

### Viktiga affärsregler att införa senare

- En anställd tillhör en butik.
- En inloggad employee är kopplad till exakt en anställd.
- En admin får bara administrera butiker den har behörighet till.
- Ett butiksschema får bara innehålla pass för anställda i samma butik.
- En anställd får bara tilldelas en passtyp som matchar anställdas roll.
- Fördelning ska ta hänsyn till `EmploymentPercentage`.
- Fördelning ska skapa förslag, inte direkt publicerade pass.
- Admin måste godkänna förslag innan de blir faktiska pass.
- Det faktiska schemat får ändras utan att grundschema eller butikens grundbehov ändras.
- Shift-byten mellan anställda ska ändra det faktiska schemat efter godkännande.
- Händelser ska påverka schemagenerering eller schemaredigering utan att skriva om grundbehovet.

### Öppna beslut innan implementation

- Ska `Role` vara global eller per butik?
- Ska `ShiftType` vara global eller per butik?
- Ska en anställd kunna arbeta i flera butiker?
- Ska en admin kunna administrera flera butiker?
- Ska första admin-kontot skapas via seed, invite eller publik registrering?
- Ska employee-konton skapas av admin eller via invitation?
- Ska butikens behovsregler ha egna tider, eller alltid kopiera tider från `ShiftType`?
- Ska butikens grundbehov ersätta dagens employee-baserade `BaseScheduleRule`, eller leva parallellt?
- Hur exakt ska `EmploymentPercentage` översättas till timmar per vecka eller per fyraveckorsperiod?
- Ska schemagenerering optimera rättvisa, kontinuitet, helger, maxpass per dag och överlapp?
- Ska händelser ligga före eller efter fördelningsförslag i flödet?

### Rekommenderad ny implementationordning

1. Inför `Store`
   - Status: infört som första skiva.
   - `Store`-modell finns.
   - `Employee` är kopplad till `Store`.
   - Enkla CRUD-endpoints finns för butik.
   - Frontend kan skapa butik och välja butik vid skapande/uppdatering av anställd.

2. Inför konton och auth-grund
   - Status: infört som första backend-skiva.
   - `UserAccount` finns.
   - Password hashing finns.
   - Login-endpoint som returnerar JWT finns.
   - Lägg till admin/employee accessroller.
   - Koppla employee-konto till `Employee`.
   - Koppla admin-konto till butik eller systemnivå.
   - Frontend API-wrapper för auth finns.
   - Kvar: skydda admin-endpoints och employee-endpoints med JWT bearer och policies.
   - Kvar: login-sida, token-lagring och frontend route guards.

3. Inför butikens grundbehov
   - Status: infört som första skiva.
   - `StoreCoverageRule` finns.
   - API finns för att lista, skapa/uppdatera och ta bort behovsregler per butik.
   - Frontend har en första vy för att skapa, ändra och ta bort butikens behov.
   - `RequiredCount` stöds så flera personer kan behövas på samma pass.

4. Bestäm relation mellan anställdas grundschema och butikens behov
   - Status: första beslut infört.
   - Butikens behov används som input för att generera anställdas `BaseScheduleRule`.
   - Första versionen genererar direkt till grundschema, utan `AssignmentProposal`.
   - Grundschemat upprepar butikens veckobehov över fyra cykelveckor.

5. Bygg fördelningstjänst
   - Status: första enkel version införd.
   - Input: butikens grundbehov, anställda, roller och sysselsättningsgrad.
   - Output: `BaseScheduleRule` direkt.
   - Matchar passtyp mot anställdas roll.
   - Försöker balansera timmar där 100% motsvarar 40 timmar per vecka.
   - Kvar: dygnsvila, helglogik, maxpass, preferenser och bättre rättvisa.

6. Lägg till admin-godkännande
   - Admin ser förslag.
   - Admin kan godkänna, avvisa eller manuellt ändra.
   - Godkända förslag skapar faktiska `Shift`.

7. Skapa allmänt schema från godkända pass och händelser
   - `Schedule` skapas per butik och period.
   - Faktiska `Shift` sparas på schemat.
   - Händelser integreras som justeringar eller blockeringar.

8. Stöd schemaändringar och byten
   - Manuell redigering ändrar endast `Shift`.
   - Shift-byten skapar begäran/förslag som admin eller berörda parter kan godkänna.
   - Inget av detta ska ändra butikens grundbehov eller anställdas grundschema.

### Rekommenderad försiktighet

Inför inte allt detta i en enda migration. Börja med `Store` och butikskoppling för anställda. Därefter bör butikens grundbehov modelleras och testas innan nuvarande `BaseScheduleRule` eventuellt ändras eller tas bort.

## Notering om JWT

ASP.NET Core rekommenderar JWT bearer authentication för API:er där klienten skickar token i `Authorization: Bearer ...`. API:t ska svara med `401` vid saknad/ogiltig token och `403` när användaren är autentiserad men saknar rätt behörighet.

För roller kan backend använda `[Authorize(Roles = "Admin")]` eller policy-baserad authorization. Policies är ofta renare om reglerna växer, till exempel om systemet senare behöver kombinera roll, verksamhet, anställnings-id eller schemastatus.

## Migration först?

Ja, första migrationen bör göras innan frontend kopplas på brett. Anledningen är att frontend-integrationen annars riskerar att byggas mot data som bara finns i minnet eller hårdkodat i komponenter.

En bra ordning är:

1. Första migrationen.
2. Verifiera backend mot riktig databas.
3. Koppla frontend till backend.
4. Lägg på JWT och accessregler.

JWT kan göras tidigare om inloggning är central för all testning, men för det här projektet är det troligen smidigare att först få domänflödet att fungera end-to-end.
