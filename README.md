# JobSearchAssistant

JobSearchAssistant is a local-first application for collecting, organizing, evaluating, and tracking job opportunities.

Its purpose is to reduce repetitive job-search administration and help people make better-informed decisions about which opportunities deserve attention. It is designed as a practical personal tool, not as a public job board, recruiting marketplace, or autonomous application agent.

## ChatGPT session identity

Use this distinct identity when starting or describing a ChatGPT session for this repository:

**JOBSEARCHASSISTANT — development of the personal job-search application**

This project concerns the application itself: its requirements, C#/.NET implementation, user experience, persistence, testing, and practical job-search workflows. It is not the Local AI Development Playbook or its supporting tooling.

When creating a session title or description, begin with **JobSearchAssistant** so it is distinguishable from the Local AI Development Playbook.

## Project status

**Local opportunity inbox implemented and deployed as a Windows Service.**

The application supports manual opportunity capture, SQLite persistence through EF Core 11, a newest-first inbox, opportunity details, required-field validation, duplicate URL warnings with explicit save confirmation, editable comments, and deletion of an opportunity.

The application is deployed locally at `http://127.0.0.1:5117/`. The Windows Service uses the persistent database at `C:\ProgramData\JobSearchAssistant\jobsearch.db`; application binaries are installed separately under `C:\Program Files\JobSearchAssistant`. The deployment script builds, tests, publishes, updates the service, and checks the HTTP health endpoint.

Latest verified automated result: solution build succeeded and all 11 tests passed. The deployment completed successfully and its health check returned HTTP 200. The user confirmed the deployed application looks correct.

## Current implementation baseline

- Solution: `JobSearchAssistant.sln`.
- Application project: `JobSearchAssistant.Web`.
- Test project: `JobSearchAssistant.Tests`.
- Target framework: .NET 11 (`net11.0`).
- UI: ASP.NET Core Razor Pages with server-rendered HTML.
- Language: C#.
- Persistence: EF Core 11 with SQLite.
- Schema management: source-controlled EF Core migrations.
- Development database: `%LOCALAPPDATA%\JobSearchAssistant\jobsearch.db`.
- Deployed database: `C:\ProgramData\JobSearchAssistant\jobsearch.db`.
- Deployed application: `C:\Program Files\JobSearchAssistant`.
- Hosting: Windows Service named `JobSearchAssistant`, running Kestrel on loopback only at `127.0.0.1:5117`.
- Deployment entry point: `scripts\Deploy.ps1` (run from an elevated PowerShell session).
- Automated tests: xUnit and ASP.NET Core integration testing with SQLite.
- Latest verified test result: 11 passed, 0 failed.

## Current opportunity-inbox capabilities

1. Manually capture a source URL and the complete original job-posting text.
2. Enter employer and job title optionally.
3. Record the capture timestamp automatically.
4. Persist opportunities in SQLite through EF Core.
5. List saved opportunities newest first.
6. Open an opportunity and review its original posting.
7. Warn when the same normalized source URL is captured again; allow explicit confirmation to save another record.
8. Save one optional free-text comments field per opportunity without changing the original posting text.
9. Delete an opportunity from its details page using a confirmed POST action.
10. Deploy updates through the PowerShell deployment script without copying application files over the persistent ProgramData database.

The application does not yet include AI evaluation, scraping, application tracking, dashboards, or automated application submission.

## Documentation

- [Requirements](docs/Requirements.md) — product needs, workflows, scope, and acceptance criteria.
- [Architecture](docs/Architecture.md) — technical direction, boundaries, persistence, and hosting.
- [Implementation Plan](docs/ImplementationPlan.md) — incremental delivery sequence and exit conditions.

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
