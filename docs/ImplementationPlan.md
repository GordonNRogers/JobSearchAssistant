# JobSearchAssistant Implementation Plan

**Status:** Opportunity inbox implemented and deployed; database backup and rollback are implemented, with operational recovery validation still open
**Last updated:** 2026-11-22

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

## Approved next implementation increment — publish an opportunity to JobSearch2026 (not started)

The user approved a single **Export to JobSearch2026** action on the opportunity detail page. This is a bounded feature, not a general GitHub integration platform.

### Design

- Generate one Markdown record using the conceptual structure in `JobSearch2026/01-JobSearch/Opportunities/_Opportunity-Template.md`.
- Preserve original posting text exactly; include captured metadata and user comments; leave evaluation and application activity blank.
- Publish to `GordonNRogers/JobSearch2026/01-JobSearch/Opportunities` through GitHub's Contents API, creating a commit on the configured branch (initially `main`).
- Authenticate with a fine-grained PAT limited to that repository and Contents read/write permission.
- Keep the Windows Service identity as `LocalService`. Store the token as a machine-scope DPAPI-protected file in `%ProgramData%\\JobSearchAssistant\\Secrets`, with ACL access restricted to LocalService and local Administrators.
- Add a local secure token-provisioning step. Never place the plaintext token in source, appsettings, SQLite, environment variables, logs, command-line arguments, or ChatGPT.
- Do not add Task Scheduler, impersonation, a PowerShell worker, local Git clone, or Git Credential Manager dependency.
- Create new files only. Make retries safe by detecting an identical existing file; never overwrite a different existing record.
- Return the GitHub file URL and commit result to the detail page.

### Implementation sequence

1. Confirm the JobSearch2026 template and destination path; define filename behavior for missing employer/title and collisions.
2. Implement secure local PAT provisioning and service-readable DPAPI/ACL storage; ensure deployment preserves the secret directory.
3. Implement a narrowly configured GitHub API client and typed publishing result/error handling.
4. Implement deterministic Markdown rendering from `JobOpportunity`, preserving original text and avoiding fabricated metadata.
5. Add the detail-page action and success/failure feedback.
6. Test rendering, filename safety, duplicate/conflict behavior, credential failures, API failures, and an end-to-end publish/retry against the private repository.

### Exit condition

A captured opportunity can be published once to the intended repository path; the GitHub file contains the expected original source and metadata; the UI displays its URL; a repeated request does not duplicate or overwrite content; credentials remain protected; and automated tests plus a real end-to-end verification pass.

This increment is approved but **not implemented**. Do not provision or request the PAT in chat; the user will create and install it locally when the provisioning workflow is ready.

## Current priority — establish and measure job discovery (operational; no application code)

Before choosing the next implementation increment, improve the actual opportunity-discovery process and collect a small amount of evidence.

- Use Glassdoor recommendations as the current qualitative benchmark. They have so far appeared better matched for .NET, senior-level, and remote opportunities, but this has not yet been measured.
- Configure comparable search families and daily alerts on Indeed, Dice, Monster, and ZipRecruiter.
- Use the target profile, four search families, bounded review routine, and two-week source comparison in [Job Discovery Workflow](JobSearchWorkflow.md).
- Compare relevant-posting yield and mismatch reasons, not just raw result counts.
- Prefer employer career pages or ATS-hosted postings as the source when available.
- Keep Glassdoor review manual. The closed HTTP retrieval experiment remains no-go for automated retrieval without express written authorization.

Do not build a scraper, AI evaluator, reporting feature, or importer just to support this measurement. A simple manual tally is sufficient. The purpose is to identify the real bottleneck and select the next small application change from observed friction.

**Exit condition:** A consistent search baseline is in use and an initial two-week comparison provides enough evidence to decide which discovery or intake improvement is worth implementing. This is an operational milestone, not a claim that application functionality has been delivered.

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

Only consider this after the current job-discovery workflow has been measured. Use the actual alerts and sources that produce relevant opportunities, rather than selecting integrations speculatively.

- Review actual job-alert sources in use, including the two-week source comparison.
- Prefer supported APIs, RSS, email alerts, or user-provided exports.
- Implement one source adapter at a time.
- Track source provenance and failures.
- Respect terms, rate limits, and access controls.
- Glassdoor remains excluded from automated retrieval absent express written authorization.

**Exit condition:** An integration demonstrably saves time or improves coverage enough to justify maintenance.

## Step 8 — Glassdoor HTTP retrieval (closed — no-go)

The separate experiment on experiment/glassdoor-http-scraping is closed. Anonymous HTTP requests returned a Cloudflare managed challenge (HTTP 403), and Glassdoor's current Terms of Use require express written permission for automated scraping/mining. No current self-service API or authorized feed for this use case was identified.

Do not implement a Glassdoor scraper, IWebScraper abstraction for this purpose, browser automation, challenge bypass, cookie replay, or automated page-content extraction without express written authorization. See docs/experiments/GlassdoorHttpScraping.md for the evidence and decision.

**Exit condition:** Closed. Reopen only if Glassdoor provides written authorization or a documented integration that expressly covers the intended access and data use.

## Step 9 — Consider a user-driven browser capture companion (deferred)

This is a convenience feature, not a scraping workaround. If capture friction proves significant, consider a small browser-extension popup or side panel that:

- Reads the active tab URL only after an explicit click.
- Displays a local capture form while the user remains on the job-board tab.
- Lets the user manually enter employer, title, location, and personal notes.
- Optionally accepts text the user manually copies and pastes into the popup, subject to the source site's content-use terms.
- Submits a draft to a loopback-only JobSearchAssistant intake endpoint for review before saving.

For Glassdoor, do not implement DOM inspection, automatic selected-text transfer, clipboard monitoring, injected scripts, or background content collection without express written permission. A click-to-read selection extension still programmatically extracts and transfers website content; user initiation does not itself establish permission. The popup can remove the need to switch to a separate JobSearchAssistant page, but it cannot remove the separate content-use question.

**Exit condition:** Build only after the user confirms that the convenience is worth a separate extension project, the local intake endpoint is designed securely, and source-site terms have been reviewed for the exact capture behavior.

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
