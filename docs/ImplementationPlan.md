# JobSearchAssistant Implementation Plan

**Status:** Opportunity-inbox increment implemented and deployed; database backup/restore hardening is the remaining deployment task
**Last updated:** 2026-11-20

## Delivery rule

Build the smallest increment that produces a useful job-search outcome. Use the tool during development rather than postponing use until a complete application exists.

Job applications, networking, and interview preparation continue independently. If development begins consuming time without improving the job search, reduce or pause it.

Do not add AI or automation merely to make a release feel more impressive. Extend the application in response to observed workflow needs.

## Step 0 — Establish the application and development baseline

### Completed and verified

- Created `JobSearchAssistant.sln`.
- Created the ASP.NET Core Razor Pages project `JobSearchAssistant.Web`, targeting `net11.0`.
- Added `JobSearchAssistant.Tests`.
- Integrated EF Core 11 and SQLite with source-controlled migrations.
- Verified solution build, automated tests, and local browser workflow.
- Confirmed direct project launch; IIS Express is not required.

.NET 11 is STS, not LTS. Keep the SDK patched and plan an upgrade before framework support ends.

**Exit condition:** .NET 11 solution builds and tests, EF Core/SQLite persistence works, and the application opens locally. Demonstrated.

## Step 1 — Build the local opportunity inbox

### Delivered

- Manual capture of source URL and complete original job-posting text; employer/title optional.
- Automatic UTC capture timestamp.
- EF Core 11 `JobOpportunity` entity, DbContext, SQLite persistence, and source-controlled migration.
- Newest-first inbox and opportunity detail page.
- Required-field validation.
- Normalized URL duplicate warning with explicit confirmation.
- Optional editable comments field on the detail page.
- Comments are persisted independently and do not alter the original posting.
- Opportunity deletion from the detail page using a confirmed POST action.
- Integration tests for comment persistence and deletion.

### Verification

Latest verified solution build succeeded and all **11 tests passed**. The deployed application was published and installed under `C:\Program Files\JobSearchAssistant`, with persistent SQLite data under `C:\ProgramData\JobSearchAssistant\jobsearch.db`.

The deployment script completed successfully, restarted the Windows Service, and received HTTP 200 from its loopback health check. The user confirmed the deployed application looked correct.

**Exit condition:** Capture, persist, list, review, comment on, and delete opportunities. Automated coverage and deployed UI check completed.

## Step 2 — Improve normalization and duplicate review

Deferred until real use demonstrates a need.

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

## Step 7 — Harden production operations

### Implemented

- Published application separate from persistent user data.
- Kestrel hosted by the `JobSearchAssistant` Windows Service.
- Service configured for automatic startup and recovery behavior.
- Loopback-only listener at `127.0.0.1:5117`.
- Production SQLite database stored at `C:\ProgramData\JobSearchAssistant\jobsearch.db`.
- `scripts\Deploy.ps1` automates build, tests, publish, application-file backup, service stop/update/start, and HTTP health verification.
- Normal deployment does not overwrite the ProgramData database or recreate the existing service.

### Remaining operational hardening

- Add a deliberate ProgramData SQLite backup before schema-changing deployment.
- Define and test restore/recovery, including the relationship between database schema migrations and binary rollback.
- Consider retaining timestamped database backups outside the application release directory.

The current script backs up application files for binary rollback; it does **not** back up the production database. The application applies EF Core migrations during Windows Service startup, so a binary rollback alone cannot reverse an applied schema migration.

**Exit condition:** A fresh install and upgrade work; application replacement does not overwrite user data; the service starts automatically and remains local-only; and a tested database backup/restore path exists. The first three have been demonstrated; database backup/restore remains open.

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
