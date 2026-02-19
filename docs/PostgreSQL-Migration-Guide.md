# MySQL to PostgreSQL Migration Guide – PDA-ACS-BE

This document lists the changes required to switch the application database from **MySQL** (Pomelo) to **PostgreSQL** (Npgsql), with task names and estimated timelines.

---

## Current Stack

| Item | Current |
|------|--------|
| Provider | Pomelo.EntityFrameworkCore.MySql 8.0 |
| Server | MySQL (e.g. 10.11.14) |
| Connection | `Server=...;Port=3306;Database=...;User Id=...;Password=...` |
| Migrations | Single init migration with MySQL-specific annotations (CharSet, ValueGenerationStrategy) |

---

## Summary of Required Changes

| # | Task | Est. Time | Priority |
|---|------|------------|----------|
| 1 | Replace MySQL package with Npgsql and register PostgreSQL in DI | 0.5–1 day | High |
| 2 | Update connection string format and config | 0.25 day | High |
| 3 | Regenerate EF Core migrations for PostgreSQL | 1–2 days | High |
| 4 | Data migration (export from MySQL, import to PostgreSQL) | 1–2 days | High |
| 5 | Smoke-test critical flows and fix any provider differences | 1–2 days | High |
| 6 | Update validation SQL doc (optional) | 0.25 day | Low |
| **Total** | | **4–8 days** | |

---

## Task 1: Replace MySQL package with Npgsql and register PostgreSQL in DI

**Estimate:** 0.5–1 day  

**Steps:**

1. **DAMS.Infrastructure.csproj**
   - Remove: `Pomelo.EntityFrameworkCore.MySql` (Version="8.0.0").
   - Add: `Npgsql.EntityFrameworkCore.PostgreSQL` (Version="8.0.0" or latest 8.x).

2. **DAMS.Infrastructure/DependencyInjection.cs**
   - Remove: `using Pomelo.EntityFrameworkCore.MySql.Infrastructure;`
   - Add: `using Npgsql.EntityFrameworkCore.PostgreSQL;`
   - Replace `UseMySql(...)` with `UseNpgsql(connectionString, npgsqlOptions => { ... })`.
   - Remove MySQL-specific options (e.g. `MySqlServerVersion`, `EnableStringComparisonTranslations`).
   - Optionally add Npgsql retry/robustness (e.g. `EnableRetryOnFailure` if available for Npgsql).

**Deliverable:** Solution builds; app starts and connects to a PostgreSQL instance (can be empty).

---

## Task 2: Update connection string format and config

**Estimate:** 0.25 day  

**Steps:**

1. **appsettings.json** (and any env-specific files)
   - Change `DefaultConnection` to a PostgreSQL connection string, e.g.:
     - `Host=47.129.240.107;Port=5432;Database=appdb_qa;Username=appuser;Password=mm@001;`
   - Note: PostgreSQL uses port **5432** by default; user key is **Username** (not `User Id`).

2. **Environment / deployment**
   - Ensure all environments (QA, staging, prod) use the new format and point to PostgreSQL.

**Deliverable:** Application connects to the correct PostgreSQL database per environment.

---

## Task 3: Regenerate EF Core migrations for PostgreSQL

**Estimate:** 1–2 days  

**Why:** Existing migrations are MySQL-specific (e.g. `MySql:CharSet`, `MySql:ValueGenerationStrategy`). PostgreSQL does not use these; the clean approach is to generate a new initial migration for Npgsql.

**Option A – New initial migration (recommended if you can afford a clean schema apply):**

1. Delete existing migration files under `DAMS.Infrastructure/Migrations/`:
   - `20260130083820_init.cs`
   - `20260130083820_init.Designer.cs`
   - `ApplicationDbContextModelSnapshot.cs`
2. Ensure DbContext and entities are unchanged (no MySQL-only APIs in code).
3. From the solution directory:
   - `dotnet ef migrations add InitialCreatePostgres --project DAMS.Infrastructure --startup-project DAMS.API`
4. Review generated migration: no MySql annotations; types (e.g. `text`, `integer`, `timestamp`) are PostgreSQL-appropriate.
5. Apply to a new PostgreSQL database:
   - `dotnet ef database update --project DAMS.Infrastructure --startup-project DAMS.API`

**Option B – Keep history and try to fix in place:**

- Possible but tedious: edit the initial migration to remove every `MySql:*` annotation and adjust type names to PostgreSQL equivalents. High risk of mistakes; not recommended unless you have a strong reason to keep the same migration history.

**Deliverable:** A single, clean “InitialCreatePostgres” (or similar) migration that applies successfully to PostgreSQL and matches your current model.

---

## Task 4: Data migration (export from MySQL, import to PostgreSQL)

**Estimate:** 1–2 days  

**Steps:**

1. **Export from MySQL**
   - Use `mysqldump` (schema + data) or a tool (e.g. pgloader, custom scripts) to export data.
   - If using `mysqldump`, you may still need to run the new EF migration on PostgreSQL first to create the schema, then migrate data only (table/column names should match if you kept PascalCase).

2. **Schema creation on PostgreSQL**
   - Prefer applying the new EF migration (Task 3) to create the schema so it stays in sync with the code.

3. **Data import**
   - Use one of:
     - **pgloader** (MySQL → PostgreSQL): can map types and copy data.
     - Custom ETL (e.g. C# console app: read from MySQL, write to PostgreSQL) for full control.
     - Export to CSV from MySQL and import with PostgreSQL `COPY` or a small script.
   - Pay attention to:
     - Identity/sequence columns (e.g. `Id`): reset sequences after insert if you care about exact IDs.
     - Dates/timestamps: ensure timezone handling is consistent.
     - String encoding (UTF-8 on both sides is typical).

4. **Identity tables**
   - If using ASP.NET Identity (AspNetUsers, AspNetRoles, etc.), include these in the export/import and reset any sequences as needed.

**Deliverable:** PostgreSQL database with schema from Task 3 and data equivalent to current MySQL.

---

## Task 5: Smoke-test critical flows and fix any provider differences

**Estimate:** 1–2 days  

**Steps:**

1. **Run the application** against the migrated PostgreSQL DB.
2. **Test:**
   - Login / Identity.
   - CRUD for main entities (e.g. Assets, Ministries, Incidents, CommonLookup, KPI results).
   - PM Dashboard (queries in `PMDashboardService`).
   - Admin Dashboard summary.
   - Incident details and KPI history.
   - Bulk asset upload (template + CSV processing).
   - Any reporting or PDF generation that hits the DB.
3. **Watch for:**
   - Case sensitivity: PostgreSQL string comparison is case-sensitive by default; if you relied on MySQL’s collation, you may need `LOWER()`/`EF.Functions.ILike` or similar in a few queries. Your code already uses `StringComparison.OrdinalIgnoreCase` in C# and `ToLower()` in LINQ in many places, which EF translates; verify any remaining filters.
   - Date/time: `DateTime.Now` in C# and `NOW()` in SQL behave similarly; ensure any `UpdatedAt`/`CreatedAt` behaviour is as expected.
   - No raw SQL was found in the C# codebase; if you add any later, keep it provider-agnostic or use conditional compilation/config for provider-specific SQL.

**Deliverable:** All critical user flows work against PostgreSQL; any provider-specific issues fixed.

---

## Task 6: Update validation SQL doc (optional)

**Estimate:** 0.25 day  

**Steps:**

- In `Docs/PM-Dashboard-Validation-Queries.sql`, replace MySQL-only syntax with PostgreSQL equivalents where needed, e.g.:
  - `DATE_SUB(NOW(), INTERVAL 30 DAY)` → `NOW() - INTERVAL '30 days'`
  - `ROW_NUMBER()` is supported in both; no change.
- Add a short note at the top that the file is for PostgreSQL (or “PostgreSQL/MySQL” if you keep both variants).

**Deliverable:** Validation queries run in a PostgreSQL client and match dashboard behaviour.

---

## Connection string reference

**MySQL (current):**
```text
Server=47.129.240.107;Port=3306;Database=appdb_qa;User Id=appuser;Password=mm@001;
```

**PostgreSQL (target):**
```text
Host=47.129.240.107;Port=5432;Database=appdb_qa;Username=appuser;Password=mm@001;
```

Optional Npgsql options (e.g. timeouts, pooling) can be added as key-value pairs in the same string or via `NpgsqlConnectionStringBuilder`.

---

## Risk and contingency

- **Timeline:** 4–8 days assumes one developer familiar with the solution and EF Core. Add buffer if multiple environments or complex data dependencies.
- **Rollback:** Keep MySQL DB and connection string available until PostgreSQL is fully validated; use feature flags or config to switch if needed.
- **Seeding:** If you use a database seeder (e.g. `DatabaseSeeder`), run it once against the new PostgreSQL DB after migration and data load to ensure reference data is consistent.

---

## Checklist before go-live

- [ ] Task 1: Npgsql package and `UseNpgsql` in DI.
- [ ] Task 2: Connection strings updated for all environments.
- [ ] Task 3: New initial migration for PostgreSQL applied.
- [ ] Task 4: Data migrated and sequences reset where needed.
- [ ] Task 5: Critical flows tested and any provider differences fixed.
- [ ] Task 6 (optional): Validation SQL doc updated for PostgreSQL.
