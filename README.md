# JobSearchAssistant

JobSearchAssistant is a local-first application for collecting, organizing, evaluating, and tracking job opportunities.

Its purpose is to reduce repetitive job-search administration and help people make better-informed decisions about which opportunities deserve attention. It is designed as a practical personal tool, not as a public job board, recruiting marketplace, or autonomous application agent.

## ChatGPT session identity

Use this distinct identity when starting or describing a ChatGPT session for this repository:

**JOBSEARCHASSISTANT — development of the personal job-search application**

This project concerns the application itself: its requirements, C#/.NET implementation, user experience, persistence, testing, and practical job-search workflows. It is not the Local AI Development Playbook or its supporting tooling.

When creating a session title or description, begin with **JobSearchAssistant** so it is distinguishable from the Local AI Development Playbook.

## Project status

**Initial ASP.NET Core scaffold committed; first product feature not yet implemented.**

The repository contains a .NET 11 Razor Pages application scaffold. The installed .NET 11 SDK, solution build, and basic local browser startup have been verified.

The first delivery remains deliberately narrow: a local job-opportunity inbox that lets a person capture a posting, preserve its original content, find it later, and detect duplicate URLs.

## Current implementation baseline

- Solution: `JobSearchAssistant.sln`.
- Application project: `JobSearchAssistant.Web`.
- Target framework: `.NET 11 (net11.0)`.
- UI: ASP.NET Core Razor Pages with server-rendered HTML.
- Language: C#.
- Current launch profiles run the project directly; IIS Express is not configured.
- The initial scaffold builds and runs locally.
- EF Core, SQLite persistence, migrations, and automated tests have not yet been added.

## First usable increment

The first increment is a local opportunity inbox:

1. Manually capture a source URL and the complete original job-posting text.
2. Allow employer and job title to be entered optionally.
3. Record the capture timestamp automatically.
4. Persist the opportunity in SQLite through EF Core.
5. List saved opportunities, newest first.
6. Open an opportunity and review its original posting.
7. Warn when the same normalized source URL is captured again.

The first increment does not include AI evaluation, scraping, application tracking, dashboards, or automated application submission.

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
