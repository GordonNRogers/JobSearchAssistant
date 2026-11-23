# JobSearchAssistant Architecture

**Status:** Opportunity inbox deployed as a local Windows Service; database backup/restore is automated, but failure-triggered rollback remains untested
**Last updated:** 2026-11-22

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

## 10. GitHub opportunity publishing (approved design; not implemented)

### User workflow

The opportunity detail page will expose an **Export to JobSearch2026** action. The application will generate one Markdown file and publish it directly to the configured branch of `GordonNRogers/JobSearch2026` in `01-JobSearch/Opportunities`. The GitHub Contents API creates the repository commit; the application does not need a local clone, Git executable, PowerShell worker, Task Scheduler task, or the user's interactive Git credentials. The initial target branch is `main` and should be configurable.

The exported record must follow the conceptual structure of `01-JobSearch/Opportunities/_Opportunity-Template.md` in JobSearch2026:

- Opportunity metadata populated only from fields JobSearchAssistant actually stores.
- Original posting text copied verbatim and unmodified.
- User comments copied into the notes section, clearly identified as user notes.
- Evaluation and application-activity sections left blank/initial, because export is not evaluation or an application.
- Unknown metadata omitted or marked unknown; never inferred or fabricated.

Use the capture date and a filesystem-safe employer/role filename following `YYYY-MM-DD-Employer-Role.md`. Handle missing employer/title without inventing values. Resolve filename collisions deterministically, but never overwrite an existing record. If a retry finds the same path and identical content, report the existing record as already published; if the content differs, return a conflict for user resolution. A successful response includes the GitHub file URL and commit information.

### GitHub authentication

Use a fine-grained personal access token owned by the user and restricted to the single `JobSearch2026` repository. Grant Contents read/write; GitHub's required repository metadata read permission may also be present. Target an approximately one-year expiration, subject to the expiration options GitHub offers for the account. Do not request broader repository or organization permissions.

The Windows Service continues to run as `LocalService`. Store the token as a DPAPI-protected file under `%ProgramData%\JobSearchAssistant\Secrets\github-pat.dpapi`, protected using `DataProtectionScope.LocalMachine`. Apply an explicit directory/file ACL granting access only to `NT AUTHORITY\LOCAL SERVICE` and local Administrators. Machine-scope DPAPI relies on the file ACL to restrict which local identities can obtain the ciphertext; do not treat DPAPI alone as the access boundary. A provisioning script/tool will prompt for the PAT securely, protect it, and write the file without putting the plaintext token in arguments, configuration, logs, or source control. The application decrypts it in memory when needed and must not log the token or authorization header. Do not store it in SQLite, appsettings, environment variables, or the repository.

If the token is missing, unreadable, expired, or rejected, publishing fails with an actionable message that does not disclose the credential. Token replacement/renewal is an explicit local provisioning operation; do not silently request broader scopes.

### API and commit behavior

Use GitHub's HTTPS Contents API (`PUT /repos/{owner}/{repo}/contents/{path}`) to create the file on the configured branch. The PAT is sent only in the HTTPS authorization header. GitHub creates the commit; this is not a local `git add`/`commit`/`push` workflow. Do not accept arbitrary repository names, paths, branches, or API URLs from the browser request. Repository, target branch, and destination folder are server-side configuration/constants. The UI submits only the captured opportunity identifier; the server loads and validates the record.

The endpoint is outbound-only to GitHub over HTTPS. The existing application remains bound to loopback (`127.0.0.1:5117`). Do not expose a generic GitHub proxy or arbitrary API caller. Restrict publishing to the fixed repository and folder. Use bounded HTTP timeouts, handle rate limits and transient failures, and distinguish authentication, permission, validation, network, and filename-conflict failures.

Publishing is an explicit user action. It does not submit a job application, alter the original captured posting, or create an evaluation. Do not automatically retry an ambiguous write without first checking whether the file was created. A successful publication should show the GitHub file URL and commit result.

### Operational boundaries

- No Task Scheduler, impersonation, service-account Git setup, local Git clone, or PowerShell publishing worker is required.
- No GitHub App or broad OAuth integration is required for this single-repository use case.
- The token must be provisioned locally by the user; never ask the user to paste it into ChatGPT.
- Deployment must preserve the secret directory and file. Normal application deployment must not overwrite or delete credentials.
- The feature is not implemented or verified until code, tests, local provisioning, and an end-to-end publish/retry test have been completed.

## 11. Future capabilities and boundaries

Potential future workflows include candidate profile management, deterministic evaluation, optional local Ollama-assisted assessment, application tracking, reporting/export, permitted source adapters, and a user-driven browser capture companion.

Keep deterministic rules authoritative for explicit constraints and known data. Treat AI output as untrusted, validate it, retain evidence and evaluator metadata, and keep the user responsible for consequential decisions.

Do not add scraping frameworks, plugin discovery, event buses, distributed messaging, multi-agent orchestration, or remote services without a demonstrated requirement. The Glassdoor HTTP experiment is closed as a no-go for automated retrieval without express written authorization. It does not justify an IWebScraper, a Glassdoor scraper, or generalized scraping infrastructure.

## 12. Optional browser capture companion (not implemented)

A browser extension may eventually reduce context switching without becoming a job-board scraper. The conservative design is a small popup or side panel that:

- Reads the active tab URL only after an explicit user action, to populate the source URL.
- Presents a local capture form for the user to enter employer, title, and any other details manually.
- Submits the user-confirmed record to a narrow local intake endpoint on 127.0.0.1.
- Creates a draft for review; it does not silently save an opportunity.

For Glassdoor, do not have the extension inspect the page DOM, read selected page text, monitor the clipboard, inject a content script, or collect data in the background without express written permission. Even if the user highlights text and clicks an extension button, the extension still programmatically reads and transfers website content; user initiation is a meaningful distinction but does not establish that this activity is permitted under Glassdoor's terms. A popup/side panel can avoid switching to a separate application page, but it cannot make automated extraction permissible.

A user could manually copy selected text and paste it into an extension popup while remaining on the Glassdoor tab. This is more convenient than navigating to the JobSearchAssistant page, but it remains copying Glassdoor content into another application and does not resolve the separate content-use restrictions. Keep the capture form usable with manually entered fields and a Glassdoor URL even when no posting text is transferred.

The extension and local intake API are future ideas only. No extension, endpoint, browser permission, or draft workflow is implemented. Before implementation, verify the browser extension's permission model, secure local-origin communication, request-forgery protections, and Glassdoor's applicable terms. Do not expose the local service beyond loopback.

## 13. Decisions still deferred

- Candidate profile and preference editing workflow.
- Cross-URL similarity detection.
- Application tracking and follow-up workflow.
- First email, RSS, or job-source integration.
- Optional AI assessment contract and provider.
- Backup retention policy and a safely tested disaster-recovery procedure.
- Authentication or multi-user access, if the single-user local model changes.
