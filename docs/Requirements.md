# JobSearchAssistant Requirements

**Status:** Local opportunity inbox implemented and deployed; additional job-search workflows remain planned
**Last updated:** 2026-11-20
**Primary objective:** Make a personal job search more efficient and better informed.

## 1. Problem statement

Finding suitable work involves repeatedly collecting and reviewing job postings, comparing them against actual experience and constraints, detecting duplicates, tracking applications, and remembering follow-up actions. The application should reduce this administrative effort and help prioritize opportunities.

The target is an individual's local workflow. This is not intended to be a commercial job board, recruiting platform, or general-purpose autonomous agent.

## 2. Product principles

- Practical job-search utility takes precedence over demonstrating technology.
- Deliver useful increments; do not wait for a complete application.
- Keep job hunting active while the tool is being developed.
- Prefer reliable, explainable deterministic processing for filtering, validation, and deduplication.
- Use AI to assist with interpretation of unstructured job descriptions, not to make unsupported claims or final decisions.
- Keep the person responsible for reviewing opportunities and submitting applications.
- Store personal candidate information locally by default.
- Respect source-site terms, access controls, and rate limits.
- Avoid paid services and cloud dependencies unless a demonstrated benefit justifies them.

## 3. Primary user and operating context

- Initial user model: one primary user.
- Environment: Windows desktop.
- Preferred implementation ecosystem: C#/.NET, Visual Studio, and SQL.
- Target framework: .NET 11 (`net11.0`).
- UI: ASP.NET Core Razor Pages, rendered on the server.
- Persistence: SQLite through Entity Framework Core 11 Code First.
- Schema evolution: source-controlled EF Core migrations.
- Development database: `%LOCALAPPDATA%\JobSearchAssistant\jobsearch.db`.
- Production database: `%ProgramData%\JobSearchAssistant\jobsearch.db`.
- Production hosting: Kestrel as a Windows Service named `JobSearchAssistant`.
- Service identity: `LocalService`.
- Local service URL: `http://127.0.0.1:5117/`.
- Installed binaries: `%ProgramFiles%\JobSearchAssistant`.
- Database backups: `%ProgramData%\JobSearchAssistant\Backups`.
- Projects: `JobSearchAssistant.Web`, `JobSearchAssistant.Tests`, and `JobSearchAssistant.DatabaseTool`.

.NET 11 and EF Core 11 are Standard Term Support (STS), not Long Term Support (LTS). Keep the SDK and EF packages on supported servicing versions and plan an upgrade before support ends. Confirm the installed Visual Studio version supports the .NET 11 SDK and tooling.

## 4. Core workflows

### WF-01 — Capture an opportunity

A person can add a job posting using supplied text and its source URL, with manually entered details when extraction is incomplete.

The system records where the posting came from and when it was captured. It must not silently invent missing employer, salary, location, or posting-date information.

**Implemented acceptance criteria**

- A posting can be captured without a live job-board integration.
- Employer and job title are optional manual fields.
- Source URL and complete original job-description text are required and retained.
- Capture time is recorded automatically.
- Saved opportunities can be listed and opened individually.
- The list is ordered newest first.
- Equivalent normalized source URLs produce a duplicate warning.
- The user can explicitly confirm saving a possible duplicate.
- Invalid or incomplete input produces validation feedback.

### WF-02 — Review and maintain an opportunity

The user can inspect the original posting and maintain personal notes about it.

**Implemented acceptance criteria**

- The detail page displays the saved opportunity and original posting.
- The user can save comments associated with an opportunity.
- The user can delete an opportunity through an explicit confirmation workflow.
- A nonexistent opportunity ID returns a not-found response.

### WF-03 — Normalize and deduplicate

The initial implementation normalizes source URLs for duplicate comparison. Cross-URL similarity detection is future work.

**Current behavior**

- URL comparison canonicalizes scheme and host, removes fragments, and trims trailing path slashes.
- Path and query casing are preserved.
- Possible same-URL duplicates require explicit user confirmation.
- Original source details remain available.

**Future acceptance criteria**

- Likely duplicates from different URLs can be identified.
- Distinct requisitions can be retained even when titles are similar.
- Uncertain matches are surfaced for human review rather than destructively discarded.

### WF-04 — Evaluate an opportunity

Future capability: compare a posting with a user-maintained candidate profile and stated job-search preferences.

Evaluation should distinguish explicit requirements from preferred qualifications, supported matches from partial matches and unknowns, demonstrated experience from learning goals, and profile evidence from model inference.

**Future acceptance criteria**

- The user can inspect why a requirement was classified as a match, gap, or unknown.
- Missing profile information is not turned into a claim of experience.
- Assessment provenance records evaluator/model and prompt or schema version where applicable.
- Invalid AI output is rejected or marked for review.
- The user can override an assessment and record a reason.

### WF-05 — Prioritize opportunities

Future capability: sort and filter opportunities by review status, fit assessment, location/work arrangement, compensation when known, capture date, and application status.

### WF-06 — Track applications

Future capability: track application status, dates, notes, contacts, interviews, and follow-up actions separately from captured opportunities.

### WF-07 — Produce a useful report

Future capability: produce a concise report of newly captured opportunities, high-priority items, items needing review, active applications, and upcoming follow-ups.

## 5. Candidate profile requirements

The future profile is user-controlled and should contain only information the user has reviewed and approved for job matching.

Potential sections include work history, demonstrated technical skills, projects and accomplishments, education, certifications, role preferences, location/work-arrangement constraints, and compensation expectations when the user chooses to record them.

Do not infer sensitive personal characteristics or store confidential information from a former employer. Keep verified profile facts distinct from aspirational learning goals.

## 6. Non-functional requirements

- **Privacy:** Local-first storage; no automatic upload of the resume or candidate profile to third parties.
- **Transparency:** Retain source URL, capture time, and future assessment provenance.
- **Reliability:** Core capture, storage, and duplicate handling must work without an LLM.
- **Recoverability:** Provide database backup, restore, and integrity verification.
- **Schema management:** Use EF Core migrations as the authoritative, source-controlled schema history.
- **Deployment isolation:** Production binaries and persistent user data are stored separately.
- **Security:** Bind the production service to loopback; do not expose it to the LAN or internet.
- **Maintainability:** Keep responsibilities separated without creating a distributed system.
- **Cost:** No mandatory paid API or subscription for the current version.
- **Security/privacy:** Do not store account passwords or automate login to job sites without a separately reviewed, explicit design. Treat browser-session cookies and other authentication artifacts as credentials; never log, commit, or store them in plaintext.

The deployment script creates and verifies a pre-deployment SQLite backup before updating an existing installation. The DatabaseTool restore operation requires the service and other database connections to be stopped. The combined deployment rollback path is implemented but has not yet been failure-injection-tested.

## 7. Explicit non-goals for the current increment

- AI evaluation, fit scoring, or candidate-profile matching.
- Production job-board scraping, scheduled collection, or unattended source integrations. The closed Glassdoor HTTP experiment does not change the current application's supported scope. A user-driven browser capture companion may be considered separately, subject to source-site terms and explicit user review.
- Application tracking, contacts, interviews, or follow-up reminders.
- Dashboards and elaborate reporting.
- Automatically submitting applications or answering employer screening questions.
- A public job board, recruiter marketplace, or multi-user SaaS.
- Complex multi-agent orchestration, microservices, message brokers, Kubernetes, or cloud hosting.
- A JavaScript-heavy frontend framework.

## 8. Delivery priorities

### Delivered — local opportunity inbox

- ASP.NET Core Razor Pages application targeting .NET 11.
- Manual capture of source URL and original job-description text; employer/title optional.
- Automatic capture timestamp.
- EF Core 11 Code First model and SQLite persistence with source-controlled migrations.
- Newest-first inbox and detail view.
- Duplicate URL warning with explicit confirmation.
- Editable optional comments field, separate from original posting text.
- Confirmed opportunity deletion through a POST handler.
- Automated integration tests for comment persistence and deletion.

Latest recorded verification: the solution build succeeded and all 11 tests passed. The application was published and deployed as a Windows Service; the deployment health check returned HTTP 200, and the user confirmed the application looked correct. These are recorded results, not a claim that the current checkout has been reverified.

### Delivered — local deployment and database utility

- Windows Service deployment on loopback.
- Separate production data directory.
- Automated build, test, publish, database backup, service update, and health check.
- SQLite backup, restore, and integrity verification utility.

### Next — useful personal workflow

- Candidate profile and preferences.
- Improved duplicate detection across different source URLs.
- Basic deterministic filtering and prioritization.
- Application status and follow-up tracking.
- Simple actionable report/export.

### Later — reduce repetitive work

- Import from user-provided files or saved email alerts.
- Optional local Ollama assessment with structured output and evidence.
- Improved requirement extraction.
- Additional legitimate source adapters where they provide clear value.

## 9. Success measures

Evaluate the tool by whether it reduces time spent collecting and reviewing postings, finds relevant opportunities, reduces duplicate review, helps prioritize applications against real constraints, prevents loss of application/follow-up details, and reveals recurring market skill requirements.

Do not treat lines of code, number of integrations, model complexity, or number of captured listings as success by themselves.

## 10. Closed experiment — Glassdoor HTTP retrieval

The Glassdoor HTTP retrieval experiment is closed as **no-go for automated retrieval without express written authorization**. Anonymous HTTP requests returned a Cloudflare managed challenge (HTTP 403); no job content was retrieved. Glassdoor's current Terms of Use, revised July 1, 2026, require express written permission for software/automated-agent scraping or mining and prohibit circumventing security features. No current, documented self-service API or authorized feed for the saved Recruiter listings was identified. See docs/experiments/GlassdoorHttpScraping.md for evidence and sources.

Do not implement a Glassdoor scraper, browser automation, challenge bypass, cookie replay, or page-content extraction without authorization. This is an engineering scope decision, not legal advice.

## 11. Future option — user-driven browser capture

A browser extension may reduce context switching without automatically extracting Glassdoor content. The conservative version is a popup or side panel that reads the active tab URL after a deliberate user action, then displays a JobSearchAssistant capture form. The user manually enters employer/title and other details and explicitly submits a draft to a local intake endpoint. This lets the user remain on the Glassdoor tab rather than navigating to the application page.

A user may also manually copy selected text and paste it into the popup. That avoids switching pages but is still copying Glassdoor content into another application; the Terms of Use contain content-use restrictions separate from the scraping prohibition. Do not assume manual copying or personal use resolves those restrictions.

Do not have a Glassdoor-specific extension inspect page DOM, automatically read selected text, monitor clipboard contents, inject page scripts, or run background content collection without express written permission. A click-to-read selection workflow is more user-directed than unattended scraping, but the extension still programmatically extracts/transfers page content; the current terms do not provide a clear safe harbor for it. Obtain clarification/permission before implementing that behavior.

Potential intake contract: source URL, employer, job title, location, user-entered notes, optional user-supplied posting text, and capture timestamp. Intake should create a reviewable draft rather than silently save an opportunity. This is a proposal only; no extension or intake endpoint exists.

## 11. Open decisions

Resolve these through small working prototypes rather than speculative design:

- Candidate-profile editing workflow.
- Which job-alert/email format is worth importing first.
- Whether local AI evaluation is sufficiently accurate and useful on real postings.
- Whether a source integration is worth maintaining compared with manual or alert-based capture.
