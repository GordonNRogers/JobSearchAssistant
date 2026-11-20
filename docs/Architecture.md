# JobSearchAssistant Architecture

**Status:** Local opportunity-inbox vertical slice deployed as a Windows Service; backup/restore automation remains outstanding
**Last updated:** 2026-11-20

## 1. Architectural objective

Build a dependable local application that supports a personal job search. Keep it straightforward to operate and maintain. Preserve a few boundaries that make likely extensions inexpensive, but do not build speculative infrastructure.

## 2. Application shape

Use a **modular monolith**: one ASP.NET Core application and one local SQLite database, with clear internal responsibilities.

```text
Development                         Deployed local application
-----------                         --------------------------
Visual Studio / dotnet run          Windows Service: JobSearchAssistant
    |                                   |
    v                                   v
ASP.NET Core / Razor Pages          Kestrel (127.0.0.1:5117)
    |                                   |
    +---------------+-------------------+
                    |
                    v
          Application workflows
             +--> Job capture/validation
             +--> URL normalization/deduplication
             +--> Opportunity list/detail
             +--> Comments and deletion
                    |
                    v
             EF Core 11 DbContext
                    |
                    v
               SQLite database
```

The current development database is stored at:

`%LOCALAPPDATA%\JobSearchAssistant\jobsearch.db`

The deployed database is stored separately from application binaries at:

`C:\ProgramData\JobSearchAssistant\jobsearch.db`

The deployed application files are installed under:

`C:\Program Files\JobSearchAssistant`

The deployed service binds only to loopback at `http://127.0.0.1:5117/`; it is not intended to be exposed to the LAN or internet. In Windows Service mode, the application selects the machine-wide Common Application Data directory and applies EF Core migrations during service startup. Development mode uses the per-user Local Application Data directory.

The solution contains two projects:

- **JobSearchAssistant.Web** — ASP.NET Core Razor Pages host, application workflows, EF Core DbContext, SQLite configuration, entity model, and migrations.
- **JobSearchAssistant.Tests** — xUnit integration tests using `WebApplicationFactory` and SQLite.

Keep this structure until actual complexity justifies further separation. Do not create a project or abstraction solely to satisfy a template.

## 3. Technology choices

- **Target framework:** .NET 11 (`net11.0`).
- **Application framework:** ASP.NET Core 11.
- **UI:** Razor Pages with server-rendered HTML.
- **Language/tooling:** C#/.NET, Visual Studio, and PowerShell on Windows.
- **Production hosting:** Kestrel hosted as a Windows Service.
- **Persistence:** SQLite with Entity Framework Core 11 Code First.
- **Schema evolution:** Source-controlled EF Core migrations.
- **Testing:** xUnit and ASP.NET Core integration testing.

.NET 11 is a Standard Term Support (STS) release rather than an LTS release. Keep the SDK and packages on supported servicing versions and plan a framework upgrade before support ends.

## 4. Core domain concept

- **JobOpportunity** — one distinct employer requisition or role posting, including source URL, optional company and title, original posting text, UTC capture timestamp, and optional user comments.

A captured opportunity is not automatically an application. Multiple source URLs may refer to the same opportunity, so source references should not be confused with the opportunity's identity.

## 5. Implemented opportunity workflow

### Capture and list

- Accept a source URL and complete original job-description text.
- Employer and job title are optional manual fields.
- Record capture time automatically.
- Preserve the submitted URL and original text.
- Validate required fields and show actionable feedback.
- List saved opportunities newest first.

### Duplicate handling

- Normalize the source URL before comparing it.
- Warn when a matching normalized URL already exists.
- Require explicit user confirmation before saving a possible duplicate.
- Current normalization canonicalizes scheme and host, removes fragments, and trims trailing path slashes. Path and query casing are preserved.
- Cross-URL similarity detection is not implemented.

### Details, comments, and deletion

- The detail page displays source metadata and the full original posting.
- One optional free-text comments field can be saved and edited independently of the original posting.
- Deletion is a POST handler and uses browser confirmation; it is not a destructive GET request.
- A missing opportunity ID returns not found.

## 6. Persistence and schema evolution

- Treat source-controlled EF Core migrations as the authoritative schema history.
- Create and test migrations during development; do not hand-edit the production database schema.
- Keep EF Core runtime, SQLite provider, Design package, and tooling on compatible 11.0.x versions.
- Keep the database outside the application release directory.
- In Windows Service mode, the application invokes `Database.Migrate()` during startup. New migrations therefore run as part of service startup; this is the current implementation, not a separate migration-bundle deployment step.
- The comments migration adds a nullable `Comments` column to `JobOpportunities`; it does not replace existing rows or the database.
- A binary rollback does not automatically reverse a database migration. Before future schema changes, preserve a verified pre-migration database backup and test upgrade/restore behavior against a copy.
- Do not introduce a remote database or synchronization service without a demonstrated requirement.

## 7. Hosting and deployment

The deployed Windows Service is named `JobSearchAssistant`, runs as `NT AUTHORITY\LocalService`, and is configured for automatic startup. Kestrel listens on `127.0.0.1:5117`.

Current paths:

```text
C:\Program Files\JobSearchAssistant\
    Published application binaries

C:\ProgramData\JobSearchAssistant\
    jobsearch.db                 # persistent SQLite database
```

The deployment entry point is `scripts\Deploy.ps1`. It must be run from an elevated PowerShell session. The script:

1. Builds the solution.
2. Runs the automated tests.
3. Publishes the application.
4. Backs up the existing application installation files under the repository's `.deploy\backup` directory.
5. Stops the Windows Service and copies the published application files into Program Files.
6. Configures service recovery behavior and starts the service.
7. Waits for the loopback listener and verifies the HTTP health endpoint.

Normal deployment does not recreate the service, reset its ACLs, or copy files into ProgramData. The explicit `-Install` switch is for first-time service installation.

The deployment script's application-file backup supports binary rollback; it is **not** a backup of the SQLite database. The script does not currently make a ProgramData database backup. Also, because migrations run at service startup, a failed binary deployment or binary rollback does not reverse a migration that has already been applied. Establish and test database backup/restore before treating schema-changing releases as fully recoverable.

## 8. Testing strategy and current evidence

The test project uses xUnit, `WebApplicationFactory`, and SQLite. The latest verified run completed with **11 passed, 0 failed, 0 skipped**.

Coverage includes:

- Empty inbox display.
- Opportunity capture and persistence in the test database.
- Required-field and URL validation.
- Duplicate URL warning, confirmation, and URL normalization behavior.
- Opportunity detail retrieval and original posting display.
- Newest-first inbox ordering.
- Saving comments, including trimming whitespace, while preserving original posting text.
- Deleting an opportunity through the POST handler and redirecting to the inbox.

The test factory uses isolated test databases. Those tests do not independently prove the contents or recoverability of the deployed ProgramData database.

The latest deployment completed successfully: build and tests passed, the Windows Service was restarted, and the loopback HTTP health check returned 200. The user confirmed the deployed UI looked correct.

## 9. Future capabilities and boundaries

### Source acquisition

Manual capture is the current source workflow. Later sources may include saved email alerts, RSS, supported APIs, and user-supplied files. Add one source adapter at a time and respect source terms, access restrictions, and rate limits. Avoid designing a generic scraping framework.

### Evaluation

When evaluation is introduced, separate deterministic checks from AI interpretation. AI may interpret unstructured descriptions and map requirements to profile evidence, but output must be structured, validated, and inspectable. Do not allow a model to invent profile facts or make authoritative decisions.

Ollama is an optional local provider, not a dependency of core workflows.

### Later domain concepts

- **CandidateProfile** — user-approved experience, skills, accomplishments, and preferences.
- **JobAssessment** — a versioned evaluation of an opportunity against a profile version.
- **ApplicationRecord** — the user's pursuit of an opportunity, with status, dates, contacts, notes, and follow-ups.

These are future concepts, not current implemented features.

## 10. Deliberately deferred

- Authentication and multi-user access.
- Candidate profile and AI assessment.
- Cross-URL similarity detection.
- Application tracking and follow-up reminders.
- Email/job-board source integrations and scheduled collection.
- LAN access, remote hosting, or cloud synchronization.
- More elaborate project/assembly separation.
- Automated database backup/restore and a tested recovery procedure.
