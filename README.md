# JobSearchAssistant

JobSearchAssistant is a local-first application for collecting, organizing, evaluating, and tracking job opportunities. Its purpose is to reduce repetitive job-search administration and help make better-informed decisions about which opportunities deserve attention.

It is a practical personal tool, not a public job board, recruiting marketplace, or autonomous application agent.

## ChatGPT session identity

Use **JobSearchAssistant** when starting or describing a ChatGPT session for this repository. This project concerns the application itself: requirements, C#/.NET implementation, user experience, persistence, testing, deployment, and practical job-search workflows. It is separate from the Local AI Development Playbook.

## Project status

**Usable local opportunity inbox deployed as a Windows Service.** The next approved increment is one-click publication of an opportunity record to the private JobSearch2026 repository; this publishing capability is designed but not implemented.

The .NET 11 Razor Pages application supports manual opportunity capture, SQLite persistence through EF Core 11, a newest-first inbox, opportunity details, required-field validation, duplicate URL warnings with explicit save confirmation, user comments, and opportunity deletion.

The application is deployed as a Windows Service and listens on loopback at `http://127.0.0.1:5117/`. Its production database is stored separately from application binaries under `%ProgramData%\JobSearchAssistant`.

The deployment process builds and tests the solution, publishes the web application, stops the service, creates and integrity-checks a SQLite backup, replaces application files, restarts the service, and performs an HTTP health check. A rollback path is implemented, but the complete failure-triggered rollback sequence has not been tested by deliberately failing a live deployment.

## Current implementation baseline

- Solution: `JobSearchAssistant.sln`.
- Web application: `JobSearchAssistant.Web`.
- Automated tests: `JobSearchAssistant.Tests`.
- SQLite utility: `JobSearchAssistant.DatabaseTool`.
- Target framework: .NET 11 (`net11.0`).
- UI: ASP.NET Core Razor Pages with server-rendered HTML.
- Language: C#.
- Persistence: EF Core 11 with SQLite and source-controlled migrations.
- Production hosting: Kestrel hosted as a Windows Service.
- Service URL: `http://127.0.0.1:5117/`.
- Installed application: `%ProgramFiles%\JobSearchAssistant`.
- Production database: `%ProgramData%\JobSearchAssistant\jobsearch.db`.
- Database backups: `%ProgramData%\JobSearchAssistant\Backups`.
- Service identity: `LocalService`.
- Automated tests: xUnit and ASP.NET Core integration testing with SQLite.

## Opportunity inbox

The implemented workflow supports:

1. Manually capture a source URL and the complete original job-posting text.
2. Optionally enter employer and job title.
3. Record the capture timestamp automatically.
4. Persist the opportunity in SQLite through EF Core.
5. List saved opportunities newest first.
6. Open an opportunity and review its original posting.
7. Warn when the same normalized source URL is captured again; require explicit confirmation to save a possible duplicate.
8. Add and save comments on an opportunity.
9. Delete an opportunity with an explicit confirmation step.

GitHub opportunity publishing is planned but not implemented. AI evaluation, application tracking, dashboards, and automated application submission are not implemented. The Glassdoor HTTP retrieval experiment on `experiment/glassdoor-http-scraping` is closed as a no-go for automated retrieval without express written authorization. A user-driven browser-extension popup or side panel is a possible future convenience for manual capture; it is not implemented.

## Planned opportunity publishing

The approved next application increment is an **Export to JobSearch2026** action on an opportunity detail page. It will render one Markdown record using the structure in `JobSearch2026/01-JobSearch/Opportunities/_Opportunity-Template.md` and publish it directly to `GordonNRogers/JobSearch2026`, under `01-JobSearch/Opportunities`, through GitHub's Contents REST API.

Authentication will use a fine-grained personal access token restricted to the `JobSearch2026` repository, with Contents read/write permission (and GitHub's required metadata read access). The Windows Service will read a DPAPI-protected token file under `%ProgramData%\\JobSearchAssistant\\Secrets`; the file and directory will be ACL-restricted to `NT AUTHORITY\\LOCAL SERVICE` and local Administrators. The token must never be stored in source control, `appsettings*.json`, the SQLite database, logs, or command-line arguments. A local provisioning step will prompt for the token and protect it with DPAPI machine scope; its implementation is part of the feature, not yet delivered.

Publishing will create a GitHub commit on the configured target branch (initially `main`) and return the resulting file URL and commit status. This is a direct repository write, not a local working-tree export followed by `git push`. The operation will create new files only: it must not overwrite an existing opportunity record. Retries must detect an already-published identical file and report it as already present; a different file at the same path is a conflict requiring user attention. The generated record must preserve the original posting text exactly and leave evaluation/application sections blank. Fields not captured by JobSearchAssistant must remain explicitly unknown or omitted rather than invented.

This is an approved design, not current functionality. No PAT has been provisioned, no GitHub API client has been implemented, and no publishing behavior has been tested.

## Current job-search focus

The immediate priority is improving the discovery of genuinely relevant opportunities, not adding AI evaluation or scraping. Glassdoor recommendations have so far appeared better matched for .NET, senior-level, and remote roles; this is a qualitative observation that still needs comparison against other sources. Use the documented search baseline and two-week source comparison in [Job Discovery Workflow](docs/JobSearchWorkflow.md). Keep Glassdoor review manual and respect the closed retrieval decision.

## Glassdoor capture boundary

Do not implement Glassdoor scraping, browser automation, page-DOM extraction, automatic selected-text transfer, clipboard monitoring, challenge bypass, or cookie replay without express written authorization. The anonymous HTTP experiment received a Cloudflare managed challenge (HTTP 403), and the current Glassdoor terms require express written permission for automated scraping/mining.

A possible future browser extension would keep the user on the Glassdoor tab and open a JobSearchAssistant capture popup or side panel. The conservative design reads only the active tab URL after an explicit user action; the user manually enters job details and submits a draft to a loopback-only intake endpoint for review. It does not read Glassdoor page content. Manual copy/paste may reduce context switching, but content reuse remains a separate terms question.

This extension and intake endpoint are proposals only. See [Glassdoor HTTP Retrieval Experiment](docs/experiments/GlassdoorHttpScraping.md) for the evidence, terms research, and design boundaries.

## Build, test, and deployment

From the repository root:

```powershell
dotnet build .\JobSearchAssistant.sln --configuration Release
dotnet test .\JobSearchAssistant.sln --configuration Release --no-build
```

Deploy or update the Windows Service from an elevated PowerShell session:

```powershell
.\scripts\Deploy.ps1
```

Use `-Install` for first-time installation:

```powershell
.\scripts\Deploy.ps1 -Install
```

The deployment script builds the solution, runs tests, builds the database utility, publishes the web application, backs up the existing application files, stops the service, creates a verified database backup when a database exists, installs the published files, starts the service, and checks the HTTP endpoint.

### SQLite DatabaseTool

`JobSearchAssistant.DatabaseTool` provides:

```powershell
dotnet run --project .\JobSearchAssistant.DatabaseTool -- backup <source.db> <backup.db>
dotnet run --project .\JobSearchAssistant.DatabaseTool -- restore <backup.db> <target.db>
dotnet run --project .\JobSearchAssistant.DatabaseTool -- verify <database.db>
```

The utility uses SQLite's online backup API and runs `PRAGMA integrity_check`. Restore stages the database in a temporary file, verifies it, and then replaces the target. Stop the application/service and ensure no other process has the database open before restoring it.

Deployment database backups are timestamped and stored under:

`%ProgramData%\JobSearchAssistant\Backups`

Do not delete the only known-good backup until a newer backup has been verified and the retention policy is understood.

## Latest recorded verification

The latest results supplied for this documentation update:

- Release solution build succeeded with 0 errors.
- Automated tests: 11 passed, 0 failed.
- `scripts\Deploy.ps1` PowerShell parser check passed.
- Deployment completed successfully.
- SQLite backup completed and passed the integrity check.
- Windows Service restarted and the HTTP health check passed.
- DatabaseTool backup/restore was tested independently using a disposable database, and the restored record was verified.

These results describe the recorded verification run; rerun the commands above after subsequent changes.

## Documentation

- [ChatGPT Session Start Prompt](docs/SessionStartPrompt.md) — repository-specific instructions and current project baseline for starting a new ChatGPT session.
- [Requirements](docs/Requirements.md) — product needs, workflows, scope, and acceptance criteria.
- [Architecture](docs/Architecture.md) — technical structure, persistence, hosting, backup, and deployment.
- [Implementation Plan](docs/ImplementationPlan.md) — delivered increments, remaining work, and exit conditions.
- [Job Discovery Workflow](docs/JobSearchWorkflow.md) — target opportunity profile, search families, manual review routine, and source-comparison experiment.
- [Glassdoor HTTP Retrieval Experiment](docs/experiments/GlassdoorHttpScraping.md) — closed experiment evidence, terms decision, and browser-assisted manual-capture proposal.

## Guiding principles

1. Deliver practical value in small, usable increments.
2. Prefer reliable, explainable deterministic processing where it is sufficient.
3. Use AI as an assistive evaluator, never as an authority.
4. Preserve source information and explain recommendations.
5. Keep people in control of applications and consequential decisions.
6. Keep personal candidate information local by default.
7. Avoid paid services and cloud dependencies unless demonstrated value justifies them.
8. Add abstractions only when they solve a real problem or enable a likely extension.
9. Respect source-site terms, access controls, and rate limits.
10. Do not let development become a substitute for the job-search work the application is intended to support.

## License

No license has been selected yet. Until a license is added, publication of this repository does not grant general permission to reuse, modify, or redistribute its contents.
