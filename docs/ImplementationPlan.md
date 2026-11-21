# JobSearchAssistant Implementation Plan

**Status:** Opportunity inbox implemented and deployed; database backup and rollback are implemented, with operational recovery validation still open
**Last updated:** 2026-11-20

## Delivery rule

Build the smallest increment that produces a useful job-search outcome. Use the tool during development rather than postponing use until a complete application exists.

Job applications, networking, and interview preparation continue independently. If development begins consuming time without improving the job search, reduce or pause it.

Do not add AI or automation merely to make a release feel more impressive. Extend the application in response to observed workflow needs.

## Step 0 — Establish the application and development baseline

### Completed

- Created `JobSearchAssistant.sln`.
- Created the ASP.NET Core Razor Pages project `JobSearchAssistant.Web`, targeting `net11.0`.
- Established direct project launch profiles for development.
- Added EF Core 11 and SQLite persistence with source-controlled migrations.
- Added `JobSearchAssistant.Tests` for automated integration testing.
- Added `JobSearchAssistant.DatabaseTool` for SQLite backup, restore, and integrity verification.
- Verified the solution build, automated tests, local browser workflow, and direct project launch; IIS Express is not required.

.NET 11 is STS, not LTS. Keep the SDK patched and plan an upgrade before framework support ends.

**Exit condition:** The .NET 11 solution builds and tests, EF Core/SQLite persistence works, and the application opens locally. Demonstrated.

## Step 1 — Build the first usable opportunity inbox

### Implemented

- Manual capture of the source URL and complete original job-posting text; employer/title optional.
- `JobOpportunity` entity, EF Core `ApplicationDbContext`, source-controlled migrations, and local SQLite initialization.
- Automatic UTC capture timestamp.
- Newest-first inbox and opportunity detail page.
- Required-field validation and normalized URL duplicate warning with explicit confirmation.
- Optional editable comments, persisted independently without changing the original posting.
- Opportunity deletion from the detail page using a confirmed POST action.
- xUnit integration tests using SQLite, including comment persistence and deletion.

### Acceptance status

The workflow supports capture, persistence, listing, detail review, duplicate handling, comments, and deletion. The automated test suite passes all 11 tests.

**Exit condition:** Capture, persist, list, review, comment on, and delete opportunities. Automated coverage and the deployed UI check completed.

## Step 2 — Establish repeatable local deployment and database recovery

### Implemented

- Windows Service named `JobSearchAssistant`, with Kestrel bound to loopback at `http://127.0.0.1:5117/`.
- Application binaries installed under `%ProgramFiles%\JobSearchAssistant`; persistent SQLite database stored under `%ProgramData%\JobSearchAssistant`.
- `scripts\Deploy.ps1` builds, tests, publishes, backs up application files, stops and updates the service, and performs a post-deployment health check.
- `JobSearchAssistant.DatabaseTool` provides SQLite backup, restore, and integrity verification.
- Deployment creates a timestamped, integrity-checked database backup before updating an existing installation and includes a rollback path for the database and previous application files if deployment fails.

### Verified

- Release solution build succeeded with 0 errors.
- All 11 automated tests passed.
- `Deploy.ps1` PowerShell syntax check passed.
- A deployment completed successfully.
- The production database backup was created and passed `PRAGMA integrity_check`.
- The service restarted and the HTTP health check passed.
- DatabaseTool backup/restore was tested independently with a disposable database; restored data was verified.

The deployed application runs as a Windows Service against the persistent `%ProgramData%` database. The latest deployment and health check confirm deployment/startup success, but do not replace a data-retention test across a deliberate service restart with a known production record.

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

## Step 8 — Investigate Glassdoor HTTP retrieval (separate experiment)

This experiment is isolated on `experiment/glassdoor-http-scraping`; it is not a production application increment and does not change the current deployed feature set.

### Planned sequence

1. Use `curl.exe` from PowerShell to request the user-provided Glassdoor Recruiter job-view URL without authentication.
2. Capture and inspect the status code, redirect chain/final URL, response headers, content type, and HTML body. Keep any response artifacts free of session secrets and personal data.
3. Determine whether the job details are present in the response, whether the page is a JavaScript-rendered shell, or whether Glassdoor requires authentication or returns an access challenge.
4. If authentication is required, investigate session handling separately. Do not assume username/password automation is appropriate; browser cookies may be cached, and session cookies must be protected as credentials.
5. If retrieval is viable and permitted, implement the equivalent request using .NET `HttpClient` and introduce a small `IWebScraper` contract with an initial Glassdoor-specific implementation.
6. Keep transport/session concerns separate from site-specific extraction. Add further site-specific strategies only when real target URLs demonstrate the need.

`curl.exe` is a diagnostic tool, not a production dependency. No extraction or authentication implementation is complete. Do not mark this step complete until the response has been inspected and a repeatable .NET implementation has been tested. Stop if access requires bypassing a challenge or other access control.

**Exit condition:** Demonstrate a permitted, repeatable `HttpClient` retrieval and extraction for the supplied Glassdoor test case, with tests for the observed response variants. If that is not viable, document the limitation and stop rather than building browser automation or speculative infrastructure.

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
