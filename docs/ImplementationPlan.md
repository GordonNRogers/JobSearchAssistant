# JobSearchAssistant Implementation Plan

**Status:** Initial foundation and opportunity-inbox implementation delivered; final persistence acceptance check pending
**Last updated:** 2026-11-19

## Delivery rule

Build the smallest increment that produces a useful job-search outcome. Use the tool during development rather than postponing use until a complete application exists.

Job applications, networking, and interview preparation continue independently. If development begins consuming time without improving the job search, reduce or pause it.

The first increment is intentionally a narrow vertical slice: capture a real posting, persist it locally, and retrieve it through a browser. Do not add AI or automation to make the first release feel more impressive.

## Step 0 — Establish the application and development baseline

### Completed and verified

- Installed .NET SDK 11.0.100 and verified it with `dotnet --list-sdks` and `dotnet --version`.
- Created `JobSearchAssistant.sln`.
- Created the ASP.NET Core Razor Pages project `JobSearchAssistant.Web`, targeting `net11.0`.
- Confirmed the solution contains the expected web project.
- Built the solution successfully with `dotnet build .\JobSearchAssistant.sln`.
- Started the application with the HTTPS project launch profile and confirmed the default page loads at the local development URL.
- Committed the initial scaffold to `main` and pushed it to GitHub. The scaffold commit is `d942836`.

The current launch profiles use `commandName: Project`; they run the ASP.NET Core application directly. IIS Express is not configured and is not a prerequisite.

### Remaining foundation work

- Confirm the installed Visual Studio version supports the .NET 11 SDK and ASP.NET Core 11 tooling, and verify build/debug from Visual Studio.

The solution build, automated test run, EF Core/SQLite integration, migration tooling, and local browser workflow have been verified.

.NET 11 is STS, not LTS. The project deliberately targets it, so keep the SDK patched and plan an upgrade before the framework reaches end of support.

**Exit condition:** The .NET 11 SDK is recognized, the solution builds and tests successfully, EF Core/SQLite tooling is ready, and the application opens locally in a browser. These conditions have been demonstrated. Visual Studio-specific build/debug verification remains optional environment confirmation.

## Step 1 — Build the first usable opportunity inbox

### Capture form

- Accept a source URL and complete original job-description text.
- Accept employer and job title as optional manual fields.
- Record capture timestamp automatically.
- Validate required fields and show useful errors.
- Preserve the original URL and posting text.

### Persistence and review

- Define the initial `JobOpportunity` entity and EF Core `DbContext`.
- Create the initial source-controlled EF Core migration.
- Initialize the local SQLite database by applying that migration.
- Save captured opportunities through EF Core.
- List saved opportunities newest first.
- Open a detail page showing the original posting and source metadata.
- Normalize source URLs consistently and warn when the same normalized URL already exists.
- Keep the database in a stable local application-data location, not the working directory by accident.

### Implementation status

Implemented:

- `JobOpportunity` entity and EF Core `ApplicationDbContext`.
- Initial source-controlled EF Core migration.
- Local SQLite database initialization.
- Opportunity capture form and validation.
- Automatic UTC capture timestamp.
- Newest-first inbox and opportunity details page.
- Normalized URL duplicate warning with explicit confirmation.
- xUnit integration tests using `WebApplicationFactory` and in-memory SQLite.

Latest verified result: solution build succeeded and all 10 tests passed.

### Remaining acceptance verification

The actual local database has been created and the initial migration applied. However, the application shutdown/restart test has not yet been completed. Do not consider the first increment fully accepted until that test passes.

### First-increment acceptance test

1. Start the application.
2. Capture a real job posting copied from a job board.
3. Confirm it appears in the opportunity list.
4. Open it and verify the original URL and complete posting text are intact.
5. Stop and restart the application.
6. Confirm the opportunity remains available.
7. Attempt to capture the same URL again and confirm a duplicate warning is shown.
8. Delete only the separate disposable test database, not %LOCALAPPDATA%\JobSearchAssistant\jobsearch.db, recreate it from the initial migration, and confirm the inbox works against the freshly migrated schema.

**Exit condition:** The user can capture, close, reopen, find, and review real postings without losing source information or creating unnoticed same-URL duplicates. The implementation and automated tests are in place. The actual application shutdown/restart verification remains outstanding.

Do not include AI evaluation, scraping, application status, dashboards, or follow-up tracking in this increment.

## Step 2 — Improve normalization and duplicate review

- Normalize URL variations.
- Extract or accept employer, title, location, work arrangement, salary, and dates when available.
- Identify likely duplicates across different URLs using conservative deterministic signals.
- Surface uncertain duplicates for human review.
- Preserve distinct requisitions even when titles are similar.

**Exit condition:** Re-importing a posting does not create an unnoticed duplicate, and distinct requisitions are not automatically merged.

## Step 3 — Add candidate profile and useful filtering

- Record user-approved experience, skills, accomplishments, preferences, and exclusions.
- Implement deterministic filters for explicit constraints.
- Support review states and prioritization.
- Keep unsupported or missing candidate information marked unknown.

**Exit condition:** The user can identify a manageable shortlist without AI.

## Step 4 — Add application tracking and reporting

- Create application records separately from captured opportunities.
- Track status, dates, contacts, notes, and follow-up actions.
- Produce a concise daily or on-demand action report.

**Exit condition:** The tool can support the actual application pipeline and identify the next actions.

## Step 5 — Evaluate local AI assistance

Only proceed after capture, profile data, and deterministic behavior are dependable.

- Define a versioned structured assessment contract.
- Use Ollama through an isolated provider.
- Ask for requirement classifications and explicit profile evidence.
- Validate output and handle unavailable models, malformed JSON, and unsupported claims.
- Compare assessments with manual review on a small sample of real postings.

**Exit condition:** AI saves review time without materially increasing false matches, invented qualifications, or missed disqualifiers. Otherwise retain deterministic filtering and defer AI.

## Step 6 — Add collection automation selectively

- Review actual job-alert sources in use.
- Prefer supported APIs, RSS, email alerts, or user-provided exports.
- Implement one source adapter at a time.
- Track source provenance and failures.
- Respect terms, rate limits, and access controls.

**Exit condition:** An integration demonstrably saves time or improves coverage enough to justify maintenance.

## Step 7 — Prepare a stable production installation

Do this after the core application is useful and reliable; it is not a prerequisite for the first increment.

- Publish a release separately from the development output.
- Install the application as a Windows Service hosting Kestrel, bound to loopback only.
- Keep releases/configuration separate from the persistent SQLite database under `%ProgramData%\JobSearchAssistant`.
- Configure service startup, restart/recovery behavior, and logging.
- Define a deployment procedure: stop service, back up database, apply the release's EF migration bundle or reviewed SQL script, switch/install the application release, then start and smoke-test the service.
- Retain the previous binary release and document database restore requirements. Do not assume binary rollback reverses a database migration.
- Test an upgrade using a copy of a database containing representative opportunity records; verify records survive.

**Exit condition:** A fresh install and an upgrade both work; application replacement does not overwrite user data; the service starts automatically and remains local-only; and a tested backup/restore path exists.

## Stop / defer conditions

Do not proceed to the next stage merely because it is listed. Defer work when:

- The preceding increment is not reliable.
- The feature does not address a recurring real task.
- A free existing service already solves the problem adequately.
- The integration is brittle or not permitted.
- Development is displacing applications, networking, or interview preparation.
- A feature adds architectural complexity without a near-term use.

## Review questions

After each useful increment:

1. Did this save time or improve job-search decisions?
2. What real postings or application workflow did it handle?
3. What failed or required manual correction?
4. Is the next feature justified by observed use?
5. Did the work reveal a genuine skill gap worth addressing?

Record implementation evidence separately from this plan. Do not mark steps complete until demonstrated.
