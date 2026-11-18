# JobSearchAssistant Architecture

**Status:** Initial architecture established; scaffold committed, product implementation not yet started  
**Last updated:** 2026-11-18

## 1. Architectural objective

Build a dependable local application that supports a personal job search. Keep it straightforward to operate and maintain. Preserve a few boundaries that make likely extensions inexpensive, but do not build speculative infrastructure.

## 2. Initial shape

Use a **modular monolith**: one ASP.NET Core application and one local database, with clear internal responsibilities.

Current scaffold and intended runtime:

```text
Development                         Intended production
-----------                         -------------------
Visual Studio / dotnet run          Windows Service
    |                                   |
    v                                   v
ASP.NET Core / Razor Pages          Kestrel (loopback only)
    |                                   |
    +---------------+-------------------+
                    |
                    v
          Application workflows
             +--> Job capture/validation
             +--> URL normalization/deduplication
             +--> Opportunity list/detail
                    |
                    v
             EF Core 11 DbContext
                    |
                    v
               SQLite database
          (stable ProgramData location)
```

The current scaffold has one project, `JobSearchAssistant.Web`, with Razor Pages and direct project launch profiles. IIS Express is not configured and is not required for the initial development workflow.

Later workflows can extend the application services without changing the local-first operating model:

```text
Application workflows
    +--> Candidate profile and preferences
    +--> Deterministic evaluation
    +--> Optional AI-assisted assessment (Ollama)
    +--> Application and follow-up tracking
    +--> Reporting/export
    |
    +--> Optional permitted job-source adapters
```

These are logical responsibilities, not a mandate to create a separate project or assembly for every box.

## 3. Technology choices

- **Target framework:** .NET 11 (`net11.0`).
- **SDK baseline:** .NET SDK 11.0.100 or a later 11.0 servicing release.
- **Application framework:** ASP.NET Core 11.
- **Initial UI:** Razor Pages with server-rendered HTML. It fits form-driven capture and review without requiring a separate frontend application.
- **Development hosting:** ASP.NET Core project launch profiles, using Kestrel through Visual Studio or `dotnet run`. IIS Express may be added if a concrete need arises; it is not currently configured.
- **Production hosting:** Kestrel hosted as a Windows Service, configured to bind only to loopback. No full IIS installation is required.
- **Persistence:** SQLite in a stable machine-wide application-data location, separate from application binaries.
- **ORM:** Entity Framework Core 11 Code First, using the Microsoft.EntityFrameworkCore.Sqlite provider.
- **Schema evolution:** EF Core migrations are committed to source control and define the database schema over time.
- **Styling:** Bootstrap or minimal CSS; no frontend framework is required.
- **Development:** C#/.NET and Visual Studio on Windows.

.NET 11 is a Standard Term Support (STS) release rather than an LTS release. This project deliberately targets the current release; keep the SDK on a supported servicing update and plan the next framework upgrade before .NET 11 support ends. Confirm the installed Visual Studio version supports the .NET 11 SDK and tooling.

The initial deployment is a locally run web application, not a cloud-hosted service. Do not bind to public network interfaces or configure external hosting for the first increment.

## 4. Solution organization

The current solution is deliberately small:

- **JobSearchAssistant.Web** — ASP.NET Core Razor Pages host. The first vertical slice can keep its application workflows, EF Core DbContext/configuration, and migrations in this project.

The current solution does not yet contain a test project. Add **JobSearchAssistant.Tests** when implementing the first product behavior so the new domain and persistence behavior can be tested. Do not create additional class libraries before the code demonstrates a need.

Extract Domain, Application, or Infrastructure projects only when boundaries become useful and the additional project structure pays for itself. Keep business rules out of PageModel code where practical, but do not introduce layers solely to satisfy a template.

## 5. Core concepts

Initial domain concept:

- **JobOpportunity** — one distinct employer requisition or role posting, including manually supplied details, original posting text, source URL, and capture metadata.

Later concepts, when their workflows are implemented:

- **CandidateProfile** — user-approved experience, skills, accomplishments, and preferences.
- **JobAssessment** — a versioned evaluation of a specific opportunity against a specific profile version.
- **ApplicationRecord** — the user's pursuit of an opportunity, with status, dates, contacts, notes, and follow-ups.

A captured opportunity is not automatically an application. Multiple source URLs may refer to the same opportunity, so source references should not be confused with the opportunity's identity.

## 6. First-increment behavior

### Capture

- Accept a source URL and complete original job-description text.
- Employer and job title are optional manual fields.
- Record capture time automatically.
- Preserve the submitted URL and original text; do not silently rewrite the source content.
- Validate required fields and show actionable validation feedback.

### Review

- List saved opportunities newest first.
- Provide a detail page that displays the original URL, capture time, optional employer/title, and full posting text.
- Allow the user to return to the list without losing data.

### Duplicate handling

- Normalize the source URL consistently before comparing it.
- Warn when a matching normalized URL already exists.
- Do not silently create a duplicate or destructively merge records.
- Cross-URL similarity detection is deferred until there is a real sample of captured postings to test against.

## 7. Important boundaries and future capabilities

### Source acquisition

The first implementation is manual capture. Later sources may include saved email alerts, RSS, supported APIs, and user-supplied files. Add one source adapter at a time and respect source terms, access restrictions, and rate limits. Avoid designing a generic scraping framework.

### Evaluation

When evaluation is introduced, separate deterministic checks from AI interpretation.

- Deterministic logic handles explicit user constraints, known fields, status, filtering, and duplicate candidates.
- AI may interpret unstructured descriptions, classify requirements, and map them to profile evidence.
- The evaluation contract should return structured findings, evidence references or excerpts, uncertainty/unknowns, and evaluator metadata.
- The application validates AI output before persistence.
- The user can inspect and override findings.

Do not make the model responsible for authoritative decisions or allow it to invent profile facts.

### Persistence and schema evolution

Use EF Core 11 Code First when persistence is introduced. Define the domain/persistence model in C# entity types and a DbContext; use Fluent API configuration where it makes mapping and constraints explicit. Use the SQLite provider (`Microsoft.EntityFrameworkCore.Sqlite`).

- Treat source-controlled EF Core migrations as the authoritative schema history.
- Create and test migrations during development; do not hand-edit the production database schema.
- Keep EF Core runtime, SQLite provider, Design package, and tooling on compatible matching 11.0.x versions. Pin the `dotnet-ef` tool in a repository-local tool manifest so developers and build environments use a known version.
- Keep database access behind application-level workflows; Razor PageModels should not become the place for complex queries or persistence policy.
- Store the production database outside the release directory, under a stable `%ProgramData%\JobSearchAssistant\Data` location. Keep backups separately under `%ProgramData%\JobSearchAssistant\Backups`.
- Back up the SQLite database before applying a production migration. Stop the Windows Service before backup/migration when required to ensure a consistent upgrade window.
- Prefer a reviewed migration bundle or reviewed SQL script for production schema updates. Do not rely on automatic `Database.Migrate()` at every production startup.
- Test upgrades against a copy of the current database. A binary rollback does not automatically reverse a schema migration; restore the pre-upgrade backup if a migration must be rolled back.
- Do not introduce a remote database or synchronization service without a demonstrated requirement.

### Local AI

Ollama is an optional local provider, not a dependency of core workflows. Put it behind an evaluation interface so the rest of the application remains useful when the model is unavailable, slow, or produces invalid output.

Record model identifier, prompt/schema version, and assessment timestamp. Treat model output as untrusted input and validate it.

### Hosting and deployment

Development currently runs through the ASP.NET Core project launch profiles. Production is a separately installed Kestrel Windows Service. The same application code and configuration model should support both hosts; hosting is an environment/deployment choice, not a separate application implementation.

Suggested production layout:

```text
C:\Program Files\JobSearchAssistant\
    Releases\<version>\       # immutable published application releases
    Current\                    # active release, if using a stable indirection

C:\ProgramData\JobSearchAssistant\
    appsettings.Production.json
    Data\JobSearchAssistant.db
    Backups\
    Logs\
```

The exact installer and release-switching mechanism can be chosen later. Requirements for any deployment mechanism:

- Application releases and user data are physically separate.
- Production binds only to loopback; do not expose the app to the LAN or internet.
- Install/update must not overwrite the database or production configuration.
- Take and verify a database backup before applying migrations.
- Apply the release's EF migration bundle or reviewed migration script as a deliberate deployment step, then start the new service version.
- Retain the previous application release for binary rollback. If the database schema has already advanced, use a tested reverse migration or restore the pre-upgrade database backup; do not assume swapping binaries is sufficient.
- Service startup/recovery behavior and logs should be configured and tested before calling the installation production-ready.

Production deployment automation is not part of the first opportunity-inbox increment.

## 8. Initial data integrity rules

- Preserve original posting text and source URL alongside normalized fields.
- Keep unknown values unknown; do not manufacture salary, dates, employer details, or requirements.
- Distinguish the job posting's lifecycle from the user's application lifecycle.
- Avoid destructive duplicate merging without user review.
- Tie future assessments to the profile and evaluation version used to produce them.
- Retain a clear audit trail for future user overrides and important status changes.

## 9. Testing strategy

First-increment tests should cover:

- Required-field validation and optional employer/title.
- URL normalization and duplicate detection.
- Preservation of original source URL and posting text.
- EF Core SQLite persistence and retrieval across application restarts.
- Initial migration creates the expected schema on a clean database.
- A subsequent migration upgrades a copy of the prior schema without losing existing opportunity records.
- Production migration procedure takes a backup before modifying the database.
- List ordering and detail retrieval.

Use deterministic fixtures. The initial test suite must not depend on a live model or external job site.

As later capabilities are added, test profile evidence mapping, structured AI response validation, application status transitions, follow-up calculations, and core workflows when Ollama is unavailable.

## 10. Extension points worth preserving

Preserve only these initial seams:

1. Job-source acquisition can grow without changing core job records.
2. Evaluation providers can change without changing application workflows.
3. Persistence can be isolated from UI workflows as the application grows.
4. Razor Pages can evolve without moving business rules into PageModel code.

Do not add plugin discovery, event buses, generic workflow engines, distributed messaging, or multi-agent orchestration unless actual requirements emerge.

## 11. Decisions deliberately deferred

- Authentication and multi-user access (not required for local single-user use).
- Installer/updater implementation and exact production release-switching mechanism.
- Remote hosting, LAN access, or cloud synchronization.
- First email or job-board source integration.
- Background collection/scheduling.
- AI provider and assessment contract.
- More elaborate project/assembly separation.
