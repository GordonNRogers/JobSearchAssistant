# JobSearchAssistant Implementation Plan

**Status:** Initial opportunity-inbox increment implemented; local Windows Service deployment operational
**Last updated:** 2026-11-20

## Delivery rule

Build the smallest increment that produces a useful job-search outcome. Use the tool during development rather than postponing use until a complete application exists.

Job applications, networking, and interview preparation continue independently. If development begins consuming time without improving the job search, reduce or pause it.

## Step 0 — Establish the application and development baseline

### Completed

- Created `JobSearchAssistant.sln`.
- Created the ASP.NET Core Razor Pages project `JobSearchAssistant.Web`, targeting `net11.0`.
- Established direct project launch profiles for development.
- Added EF Core 11 and SQLite persistence with source-controlled migrations.
- Added `JobSearchAssistant.Tests` for automated integration testing.
- Added `JobSearchAssistant.DatabaseTool` for SQLite backup, restore, and integrity verification.

## Step 1 — Build the first usable opportunity inbox

### Implemented

- `JobOpportunity` entity and EF Core `ApplicationDbContext`.
- Source-controlled EF Core migrations.
- Local SQLite database initialization.
- Opportunity capture form with required-field validation and optional employer/title.
- Automatic UTC capture timestamp.
- Newest-first inbox and opportunity detail page.
- Normalized URL duplicate warning with explicit confirmation.
- Save and update opportunity comments.
- Delete opportunity with confirmation.
- xUnit integration tests using SQLite.

### Acceptance status

The implemented workflow supports capture, persistence, listing, detail review, duplicate handling, comments, and deletion. The current automated test suite passes 11 tests.

The earlier first-increment restart check is superseded by the operational deployment: the application is running as a Windows Service against the persistent `%ProgramData%` database. The latest deployment completed and passed its HTTP health check. This verifies that deployment/startup succeeded; it is not a substitute for a separately documented data-retention test across a deliberate service restart with a known production record.

## Step 2 — Establish repeatable local deployment and database recovery

### Implemented

- Windows Service named `JobSearchAssistant`.
- Kestrel bound to loopback at `http://127.0.0.1:5117/`.
- Application binaries installed under `%ProgramFiles%\JobSearchAssistant`.
- Persistent SQLite database stored under `%ProgramData%\JobSearchAssistant`.
- `scripts\Deploy.ps1` builds, tests, publishes, backs up application files, stops and updates the service, and performs a post-deployment health check.
- `JobSearchAssistant.DatabaseTool` provides SQLite backup, restore, and integrity verification.
- Deployment creates a timestamped, integrity-checked database backup before updating an existing installation.
- Deployment contains a rollback path for restoring the database and previous application files if deployment fails.

### Verified

- Release solution build succeeded with 0 errors.
- All 11 automated tests passed.
- `Deploy.ps1` PowerShell syntax check passed.
- A deployment completed successfully.
- The production database backup was created and passed `PRAGMA integrity_check`.
- The service restarted and the HTTP health check passed.
- DatabaseTool backup/restore was tested independently with a disposable database; restored data was verified.

### Not yet verified

- The complete deployment rollback path has not been failure-injection-tested.
- A formal disaster-recovery exercise using a production backup has not been performed.
- Backup retention and cleanup policy has not been defined.

Do not deliberately break the live installation to test rollback. If rollback testing is pursued, use a disposable service installation and database.

## Step 3 — Improve normalization and duplicate review

- Normalize URL variations consistently.
- Extract or accept employer, title, location, work arrangement, salary, and dates when available.
- Identify likely duplicates across different URLs using conservative deterministic signals.
- Surface uncertain duplicates for human review.
- Preserve distinct requisitions even when titles are similar.

**Exit condition:** Re-importing a posting does not create an unnoticed duplicate, and distinct requisitions are not automatically merged.

## Step 4 — Add candidate profile and useful filtering

- Record user-approved experience, skills, accomplishments, preferences, and exclusions.
- Implement deterministic filters for explicit constraints.
- Support review states and prioritization.
- Keep unsupported or missing candidate information marked unknown.

**Exit condition:** The user can identify a manageable shortlist without AI.

## Step 5 — Add application tracking and reporting

- Create application records separately from captured opportunities.
- Track status, dates, contacts, notes, and follow-up actions.
- Produce a concise daily or on-demand action report.

**Exit condition:** The tool can support the actual application pipeline and identify the next actions.

## Step 6 — Evaluate local AI assistance

Only proceed after capture, profile data, and deterministic behavior are dependable.

- Define a versioned structured assessment contract.
- Use Ollama through an isolated provider.
- Ask for requirement classifications and explicit profile evidence.
- Validate output and handle unavailable models, malformed JSON, and unsupported claims.
- Compare assessments with manual review on a small sample of real postings.

**Exit condition:** AI saves review time without materially increasing false matches, invented qualifications, or missed disqualifiers. Otherwise retain deterministic filtering and defer AI.

## Step 7 — Add collection automation selectively

- Review actual job-alert sources in use.
- Prefer supported APIs, RSS, email alerts, or user-provided exports.
- Implement one source adapter at a time.
- Track source provenance and failures.
- Respect terms, rate limits, and access controls.

**Exit condition:** An integration demonstrably saves time or improves coverage enough to justify maintenance.

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
