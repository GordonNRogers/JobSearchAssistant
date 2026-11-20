# JobSearchAssistant Requirements

**Status:** Local opportunity inbox implemented and deployed; operational backup/restore procedure remains to be established
**Last updated:** 2026-11-20
**Primary objective:** Make a personal job search more efficient and better informed.

## 1. Problem statement

Finding suitable work involves repeatedly collecting and reviewing job postings, comparing them against actual experience and constraints, detecting duplicates, tracking applications, and remembering follow-up actions. The application should reduce this administrative effort and help prioritize opportunities.

The initial target is an individual's local workflow. This is not initially intended to be a commercial job board, recruiting platform, or general-purpose autonomous agent.

## 2. Product principles

- Practical job-search utility takes precedence over demonstrating technology.
- Deliver useful increments; do not wait for a complete application.
- Keep job hunting active while the tool is being developed.
- Prefer reliable, explainable deterministic processing for filtering, validation, and deduplication.
- Use AI to assist with interpretation of unstructured job descriptions, not to make unsupported claims or final decisions.
- Keep the person responsible for reviewing opportunities and submitting applications.
- Store personal candidate information locally by default.
- Respect source-site terms, access controls, and rate limits. Do not assume unrestricted scraping is permitted.
- Avoid paid services and cloud dependencies unless a demonstrated benefit justifies them.

## 3. Primary user and operating context

- Initial user model: one primary user.
- Initial environment: Windows desktop.
- Preferred implementation ecosystem: C#/.NET, Visual Studio, and SQL.
- **Target framework:** .NET 11 (`net11.0`).
- **SDK:** .NET 11 SDK, baseline 11.0.100.
- Persistence: SQLite through Entity Framework Core 11 Code First.
- Schema evolution: source-controlled EF Core migrations.
- Initial user interface: ASP.NET Core Razor Pages, rendered on the server.
- Development hosting: ASP.NET Core project launch profiles using direct project launch. IIS Express is not configured.
- Deployed hosting: Kestrel as a Windows Service, bound to `127.0.0.1:5117`.
- Development database: `%LOCALAPPDATA%\JobSearchAssistant\jobsearch.db`.
- Deployed database: `C:\ProgramData\JobSearchAssistant\jobsearch.db`.
- Deployed binaries: `C:\Program Files\JobSearchAssistant`.
- No initial requirement for multi-user access, public hosting, or mobile support.

.NET 11 and EF Core 11 are Standard Term Support (STS), not Long Term Support (LTS). Keep the SDK and EF packages on supported servicing versions and plan an upgrade before support ends. Confirm the installed Visual Studio version supports the .NET 11 SDK and tooling.

## 4. Core workflows

### WF-01 — Capture and maintain an opportunity

A person can add a job posting using supplied text and its source URL, with manually entered details when extraction is incomplete.

The system records where the posting came from and when it was captured. It must not silently invent missing employer, salary, location, or posting-date information.

**Acceptance criteria**

- A posting can be captured without requiring a live integration with a job board.
- Employer and job title can be entered manually but are not required if unknown.
- The source URL and complete original job-description text are required and retained.
- Capture time is recorded automatically.
- Saved opportunities can be listed and opened individually; the list is ordered newest first.
- Re-entering an equivalent normalized source URL produces a clear duplicate warning. The user can explicitly confirm saving another record.
- Invalid or incomplete input produces actionable validation feedback.
- The user can add or edit one optional free-text comments field on an opportunity.
- Comments persist independently; saving comments does not modify the original posting text.
- The user can delete an opportunity from its details page. Deletion uses a POST action and requires browser confirmation.
- Existing opportunities remain available after application restart and application deployment.

### WF-02 — Normalize and deduplicate

The system extracts or accepts common fields: employer, title, location/work arrangement, compensation when stated, posting date when stated, description, requirements, and application URL.

It identifies likely duplicate postings using URL normalization and a combination of employer, title, location, and description similarity or fingerprint. A possible duplicate is surfaced for review rather than destructively discarded.

**Acceptance criteria**

- Re-importing the same source URL does not create an unnoticed duplicate.
- Likely duplicates from different URLs can be identified.
- Distinct requisitions can be retained even when titles are similar.
- Original source details remain available.

### WF-03 — Evaluate an opportunity

The system compares a posting with a user-maintained candidate profile and stated job-search preferences.

Evaluation should distinguish explicit requirements from preferred qualifications, supported matches from partial matches and unknowns, demonstrated experience from learning goals, potential disqualifiers from ordinary skill gaps, and profile evidence from model inference.

The assessment should provide reasons and supporting excerpts, not just a numeric score. Any score is a prioritization aid, not a probability of being hired.

### WF-04 — Prioritize opportunities

The user can sort and filter opportunities by review status, fit assessment, location/work arrangement, compensation when known, capture date, and application status. The tool should make the next useful action visible.

### WF-05 — Track applications

For opportunities the user chooses to pursue, the system tracks application status and relevant dates, notes, contacts, interviews, and follow-up actions. Saving a posting does not mean an application was submitted.

### WF-06 — Produce a useful report

The system can produce a concise report of newly captured opportunities, high-priority items, items needing review, active applications, and upcoming follow-ups. The report should support action rather than encourage activity-volume metrics.

## 5. Candidate profile requirements

The profile is user-controlled and should contain only information the user has reviewed and approved for job matching. Potential sections include work history, demonstrated technical skills, projects, education, role preferences, location/work-arrangement constraints, and compensation expectations where the user chooses to record them.

Do not infer sensitive personal characteristics. Do not store confidential information from a former employer. Keep verified profile facts distinct from aspirational learning goals.

## 6. Non-functional requirements

- **Privacy:** Local-first storage; no automatic upload of the resume or candidate profile to third parties.
- **Transparency:** Retain source URL, capture time, and assessment provenance.
- **Reliability:** Core capture, storage, deduplication, comments, and deletion must work without an LLM.
- **Recoverability:** Define and test a production database backup and restore procedure. The current deployment script does not yet back up the ProgramData database.
- **Schema management:** Use EF Core 11 Code First migrations as the authoritative, source-controlled database schema history. Do not recreate or replace the production database during application upgrades.
- **Deployment isolation:** Production binaries/configuration must be separate from persistent user data. An application release must not overwrite the SQLite database.
- **Maintainability:** Keep responsibilities separated without creating a distributed system.
- **Cost:** No mandatory paid API or subscription for the initial version.
- **Security:** Protect local data appropriately; do not store account passwords or automate login to job sites.

## 7. Explicit non-goals for the current increment

- AI evaluation, fit scoring, or candidate-profile matching.
- Job-board scraping, scheduled collection, or source integrations.
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

Latest verified result: solution build succeeded; all 11 tests passed. The application was published and deployed as a Windows Service. The deployment health check returned HTTP 200, and the user confirmed the application looked correct.

### MVP — useful personal workflow

1. Candidate profile and preferences in a user-editable form.
2. Improved duplicate detection across different source URLs.
3. Basic deterministic filtering and prioritization.
4. Application status and follow-up tracking.
5. Simple actionable report/export.

### Next — reduce repetitive work

1. Import from user-provided files or saved email alerts.
2. Optional local Ollama assessment with structured output and evidence.
3. Improved requirement extraction.
4. Additional legitimate source adapters where they provide clear value.

### Later — only when justified by observed use

- Scheduled collection from permitted sources.
- More advanced reporting and search analytics.
- Resume/cover-letter assistance with explicit user review.
- Alternative hosting or user interface if actual usage requires it.

## 9. Success measures

Evaluate the tool by whether it reduces time spent collecting and reviewing postings, finds relevant opportunities, reduces duplicate review, helps prioritize applications against real constraints, prevents loss of application/follow-up details, and reveals recurring market skill requirements.

Do not treat lines of code, number of integrations, model complexity, or number of captured listings as success by themselves.

## 10. Open decisions

Resolve these through small working prototypes rather than speculative design:

- Production database backup and restore procedure, including pre-migration backup and recovery testing.
- Exact candidate-profile editing workflow.
- Which job-alert/email format is worth importing first.
- Whether local AI evaluation is sufficiently accurate and useful on real postings.
- Whether a source integration is worth maintaining compared with manual or alert-based capture.
