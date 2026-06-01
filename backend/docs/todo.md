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
     - `PUT /api/employees/{id}/shift-types`
     - `GET /api/employees/{id}/details`
     - `GET /api/schedules`
     - `POST /api/schedules/generate`
   - Mappa frontendens rollval till `roleId` och anställning till `employmentPercentage`.

5. JWT, inloggning och access
   - Inför auth efter att grundflödet fungerar mot databas och API, om inte projektet behöver multi-user-säkerhet direkt.
   - Lägg till användar-/konto-modell separat från `Employee` eller besluta tydligt om anställda också är inloggningskonton.
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
