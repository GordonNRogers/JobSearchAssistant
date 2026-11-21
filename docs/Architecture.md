# JobSearchAssistant Architecture

**Status:** Opportunity inbox deployed as a local Windows Service; database backup/restore is automated, but failure-triggered rollback remains untested
**Last updated:** 2026-11-20

## 1. Architectural objective

Build a dependable local application that supports a personal job search. Keep it straightforward to operate and maintain. Preserve only the boundaries that make likely extensions inexpensive; do not build speculative infrastructure.

## 2. System shape

The application is a modular monolith: one ASP.NET Core application and one local SQLite database, with a separate command-line utility for database backup, restore, and integrity verification.

```text
Browser (same machine)
    |
    | http://127.0.0.1:5117/
    v
Windows Service: JobSearchAssistant (LocalService)
    |
    v
ASP.NET Core / Kestrel / Razor Pages
    |
    v
Application workflows and EF Core 11
    |
    v
SQLite: %ProgramData%\JobSearchAssistant\jobsearch.db

Deployment script
    +--> build, test, publish
    +--> stop service
    +--> DatabaseTool backup + integrity check
    +--> replace installed application files
    +--> start service + HTTP health check
    +--> rollback path on deployment failure
```

Development uses ASP.NET Core project launch profiles and a local development database under `%LOCALAPPDATA%\JobSearchAssistant\jobsearch.db`. Production uses `%ProgramData%\JobSearchAssistant\jobsearch.db`, separate from installed binaries.

## 3. Technology choices

- **Target framework:** .NET 11 (`net11.0`).
- **Application framework:** ASP.NET Core 11.
- **UI:** Razor Pages with server-rendered HTML.
- **Development hosting:** Direct ASP.NET Core project launch through Visual Studio or `dotnet run`.
- **Production hosting:** Kestrel hosted as a Windows Service.
- **Network binding:** Loopback only; service URL `http://127.0.0.1:5117/`.
- **Persistence:** SQLite.
- **ORM:** Entity Framework Core 11 with the SQLite provider.
- **Schema evolution:** Source-controlled EF Core migrations.
- **Database utility:** `Microsoft.Data.Sqlite` command-line project using SQLite backup facilities and `PRAGMA integrity_check`.
- **Tests:** xUnit and ASP.NET Core integration testing.

.NET 11 is an STS release. Keep the SDK and packages on supported servicing versions and plan a framework upgrade before support ends.

## 4. Solution organization

The solution contains three projects:

- **JobSearchAssistant.Web** — ASP.NET Core Razor Pages host, application workflows, EF Core DbContext, SQLite configuration, entity model, and migrations.
- **JobSearchAssistant.Tests** — xUnit tests, including ASP.NET Core integration tests using SQLite.
- **JobSearchAssistant.DatabaseTool** — command-line backup, restore, and integrity-check utility.

The structure remains deliberately small. Do not extract additional class libraries until actual complexity justifies them.

## 5. Current domain and workflows

`JobOpportunity` represents one captured job posting. It includes the source URL, optional company and title, original posting text, capture timestamp, and optional user comments.

Implemented workflows:

- Manual capture with required-field validation.
- Duplicate URL detection and explicit confirmation before saving a possible duplicate.
- Newest-first inbox and opportunity detail view.
- Save/update comments.
- Delete an opportunity with confirmation.

A captured opportunity is not automatically an application. Application tracking remains a future capability.

## 6. Persistence and schema evolution

- EF Core 11 Code First migrations are the authoritative, source-controlled schema history.
- The application applies pending migrations through `db.Database.Migrate()` during startup, including Windows Service startup.
- The production database is `%ProgramData%\JobSearchAssistant\jobsearch.db`.
- The service runs as `LocalService`; deployment setup grants the service identity the required access to the data directory.
- Application binaries are installed under `%ProgramFiles%\JobSearchAssistant`, separate from persistent data.
- Do not replace or delete the production database as part of an ordinary application update.

A database schema change can make a binary-only rollback insufficient. The deployment script therefore creates a pre-deployment database backup and includes a restore path if deployment fails. The restore path requires the service to be stopped and no other database connections to remain open.

## 7. DatabaseTool

Project: `JobSearchAssistant.DatabaseTool`

Commands:

```text
backup <source.db> <backup.db>
restore <backup.db> <target.db>
verify <database.db>
```

Behavior:

- `backup` uses SQLite's backup API rather than copying a potentially active database file.
- `verify` runs `PRAGMA integrity_check`.
- `restore` writes to a temporary database in the target directory, closes the database connections, verifies the temporary database, and replaces the target.
- Database connections disable pooling so handles are released before replacement.
- Restore is an offline operation. Stop the Windows Service and ensure no other process has the database open before running it.
- The deployment script stores timestamped backups under `%ProgramData%\JobSearchAssistant\Backups`.

The utility's backup/restore flow was tested independently against a disposable database, including verification of a restored record.

## 8. Hosting and deployment

### Installed layout

```text
%ProgramFiles%\JobSearchAssistant\
    JobSearchAssistant.Web.exe
    ...published application files...

%ProgramData%\JobSearchAssistant\
    jobsearch.db
    Backups\
        jobsearch-<timestamp>.db
```

The Windows Service is named `JobSearchAssistant`, runs as `LocalService`, and serves the application at `http://127.0.0.1:5117/`.

### Deployment script

`scripts\Deploy.ps1` is the deployment entry point. It requires an elevated PowerShell session.

The implemented deployment sequence:

1. Validate the repository and deployment prerequisites.
2. Build the solution and run automated tests.
3. Build `JobSearchAssistant.DatabaseTool`.
4. Publish the web application.
5. Back up the currently installed application files.
6. Stop the Windows Service.
7. Create a SQLite backup when a production database exists; verify the backup with `PRAGMA integrity_check`.
8. Copy published application files into the installation directory.
9. Start the service.
10. Check service state, loopback listener, and HTTP response.

The first-install path creates the service and data directories and configures the service account's data-directory access. A first install has no existing database to back up.

### Rollback

If deployment fails after the service has been stopped, the script attempts to stop the service, restore the pre-deployment database backup when one was created, restore the previous application files, restart the service, and verify health. If rollback cannot be completed, the service is left stopped rather than intentionally starting an unverified combination.

The normal deployment and successful backup/health-check path have been exercised. The complete failure-triggered rollback sequence has **not** been tested through deliberate failure injection. Do not describe rollback as proven until it has been safely tested against a disposable installation and database.

## 9. Testing and verification

The current automated test suite uses xUnit and ASP.NET Core integration testing with SQLite. The latest recorded run passed 11 tests with no failures.

The latest recorded operational verification also includes:

- Release solution build succeeded with 0 errors.
- PowerShell parser check passed for `scripts\Deploy.ps1`.
- Deployment completed successfully.
- Timestamped SQLite backup passed the integrity check.
- Windows Service restarted and HTTP health check passed.
- DatabaseTool backup/restore independently passed a disposable-database test with restored-record verification.

These checks do not establish that the combined deployment rollback path has been failure-tested.

## 10. Future capabilities and boundaries

Potential future workflows include candidate profile management, deterministic evaluation, optional local Ollama-assisted assessment, application tracking, reporting/export, and permitted source adapters.

Keep deterministic rules authoritative for explicit constraints and known data. Treat AI output as untrusted, validate it, retain evidence and evaluator metadata, and keep the user responsible for consequential decisions.

Do not add scraping frameworks, plugin discovery, event buses, distributed messaging, multi-agent orchestration, or remote services without a demonstrated requirement. The separate Glassdoor HTTP experiment is intentionally narrower: a small `IWebScraper` contract, a site-specific implementation, and shared `HttpClient` infrastructure only as evidence requires.

## 11. Experimental architecture — Glassdoor HTTP retrieval

This work is isolated on the `experiment/glassdoor-http-scraping` branch. It is not part of the deployed application and must not be described as an implemented product capability.

### Intended shape

- **`IWebScraper`** is the application-facing abstraction for retrieving and extracting job details from a supplied posting URL.
- **Glassdoor implementation** is the first site-specific scraper, contingent on proving that the supplied Glassdoor Recruiter job-view page can be retrieved and parsed reliably.
- **Scraper selection** should be based on the target URL/site and allow additional site-specific implementations later. Do not design a plugin marketplace or speculative site knowledge base now.
- **HTTP transport** uses .NET `HttpClient`. `curl.exe` is for manual diagnostics and request/response inspection, not a runtime dependency.
- **Session/authentication** is a separate concern from page parsing. A future session abstraction may own cookies and authenticated request state; the scraper should not own credential storage.

### Investigation sequence

1. Request the supplied Glassdoor URL anonymously with `curl.exe`.
2. Record status, redirect behavior/final URI, relevant response headers, content type, and a safely stored response body.
3. Inspect whether job data is in the returned HTML or whether the response is a sign-in page, challenge, or JavaScript shell.
4. Only if required, investigate an authenticated request and the minimum session mechanism needed.
5. Reproduce any viable request with .NET `HttpClient`, then implement extraction behind `IWebScraper`.

The user can view the URL in a normal browser, but browser authentication may be cached. This does not establish that an anonymous HTTP request will succeed. No HTTP response has yet been captured, no extraction has been demonstrated, and no authentication design has been selected.

### Security and operational constraints

- Treat cookies, session identifiers, and credentials as secrets.
- Never commit secrets or raw authenticated response artifacts.
- Do not place cookies in PowerShell command history or diagnostic logs.
- If persistence is needed, evaluate OS-protected storage (for example, Windows DPAPI) rather than plaintext files; do not implement persistence until the session requirement is demonstrated.
- Let `HttpClientHandler` handle ordinary redirects and cookie-container behavior where appropriate, but inspect the actual response rather than assuming site behavior.
- Respect site terms, access controls, and rate limits. Do not bypass CAPTCHA, bot challenges, authentication controls, or other restrictions.

## 12. Decisions still deferred

- Candidate profile and preference editing workflow.
- Cross-URL similarity detection.
- Application tracking and follow-up workflow.
- First email, RSS, or job-source integration.
- Optional AI assessment contract and provider.
- Backup retention policy and a safely tested disaster-recovery procedure.
- Authentication or multi-user access, if the single-user local model changes.
