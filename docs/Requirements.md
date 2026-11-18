# JobSearchAssistant Requirements

**Status:** Initial requirements baseline; first product increment not yet implemented  
**Last updated:** 2026-11-18  
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
- **SDK:** .NET 11 SDK, baseline 11.0.100. SDK 11.0.100 is installed and has been verified.
- Initial persistence: SQLite, accessed through Entity Framework Core 11 Code First. EF Core and SQLite have not yet been added to the scaffold.
- Schema evolution: source-controlled EF Core migrations.
- Initial user interface: ASP.NET Core Razor Pages, rendered on the server.
- Current development hosting: ASP.NET Core project launch profiles using direct project launch. IIS Express is not currently configured.
- Intended production hosting: Kestrel as a Windows Service, bound to loopback and accessible only from the local machine.
- No initial requirement for multi-user access, public hosting, or mobile support.

The current repository scaffold consists of `JobSearchAssistant.sln` and one ASP.NET Core project, `JobSearchAssistant.Web`. The solution build and basic local browser startup have been verified. Product functionality, persistence, and automated tests remain to be implemented.

.NET 11 and EF Core 11 are Standard Term Support (STS), not Long Term Support (LTS). The project accepts the shorter support lifecycle in exchange for targeting the current releases. Keep the SDK and EF packages on supported servicing versions and plan an upgrade before support ends. Confirm the installed Visual Studio version supports the .NET 11 SDK and tooling.

## 4. Core workflows

### WF-01 — Capture an opportunity

A person can add a job posting using supplied text and its source URL, with manually entered details when extraction is incomplete.

The system records where the posting came from and when it was captured. It must not silently invent missing employer, salary, location, or posting-date information.

**First-increment acceptance criteria**

- A posting can be captured without requiring a live integration with a job board.
- Employer and job title can be entered manually but are not required if unknown.
- The source URL and complete original job-description text are required and retained.
- Capture time is recorded automatically.
- Saved opportunities can be listed and opened individually.
- The list is ordered newest first.
- Re-entering the same normalized source URL produces a clear duplicate warning rather than an unnoticed second record.
- Invalid or incomplete input produces actionable validation feedback.
- The application can be closed and reopened without losing saved opportunities.

The first increment is a capture-and-review inbox. It does not include application status or imply that a saved opportunity has been applied to.

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

Evaluation should distinguish:

- Explicit requirements from preferred qualifications.
- Supported matches from partial matches and unknowns.
- Demonstrated experience from skills the candidate merely wants to learn.
- Potential disqualifiers from ordinary skill gaps.
- Evidence in the candidate profile from inference made by a model.

The assessment should provide reasons and supporting excerpts, not just a numeric score. Any score is a prioritization aid, not a probability of being hired.

**Acceptance criteria**

- The user can inspect why a requirement was classified as a match, gap, or unknown.
- Missing profile information is not turned into a claim of experience.
- The assessment records the evaluator/model and prompt or schema version where applicable.
- Invalid AI output is rejected or marked for review; it is not silently accepted.
- The user can override an assessment and record a reason.

### WF-04 — Prioritize opportunities

The user can sort and filter opportunities by review status, fit assessment, location/work arrangement, compensation when known, capture date, and application status.

The tool should make the next useful action visible: review, research employer, prepare application, follow up, or close out.

### WF-05 — Track applications

For opportunities the user chooses to pursue, the system tracks application status and relevant dates, notes, contacts, interviews, and follow-up actions.

Initial statuses should be simple and editable: Interested, Researching, Ready to Apply, Applied, Interviewing, Offer, Rejected, Withdrawn, and Closed/Expired.

An opportunity and an application are related but distinct: saving a posting does not mean an application was submitted.

### WF-06 — Produce a useful report

The system can produce a concise report of newly captured opportunities, high-priority items, items needing review, active applications, and upcoming follow-ups.

The report should support action rather than encourage activity-volume metrics.

## 5. Candidate profile requirements

The profile is user-controlled and should contain only information the user has reviewed and approved for job matching.

Potential sections:

- Work history and responsibilities.
- Demonstrated technical skills and proficiency/evidence.
- Projects and accomplishments.
- Education and certifications.
- Role preferences and exclusions.
- Location, remote/hybrid, travel, and work-authorization constraints where relevant.
- Compensation expectations, when the user chooses to record them.

Do not infer sensitive personal characteristics. Do not store confidential information from a former employer. Keep a clear distinction between verified profile facts and aspirational learning goals.

## 6. Non-functional requirements

- **Privacy:** Local-first storage; no automatic upload of the resume or candidate profile to third parties.
- **Transparency:** Retain source URL, capture time, and assessment provenance.
- **Reliability:** Core capture, storage, deduplication, and tracking must work without an LLM.
- **Recoverability:** Production database backup and restore must be defined before production use; upgrades must take a pre-migration backup.
- **Schema management:** Use EF Core 11 Code First migrations as the authoritative, source-controlled database schema history. Do not recreate or replace the production database during application upgrades.
- **Deployment isolation:** Production binaries/configuration must be separate from persistent user data. A new application release must not overwrite the SQLite database.
- **Maintainability:** Keep responsibilities separated without creating a distributed system.
- **Cost:** No mandatory paid API or subscription for the initial version.
- **Security:** Protect local data appropriately; do not store account passwords or automate login to job sites.

## 7. Explicit non-goals for the first increment

- AI evaluation, fit scoring, or candidate-profile matching.
- Job-board scraping, scheduled collection, or source integrations.
- Application tracking, contacts, interviews, or follow-up reminders.
- Dashboards and elaborate reporting.
- Automatically submitting applications or answering employer screening questions.
- A public job board, recruiter marketplace, or multi-user SaaS.
- Resume rewriting or cover-letter generation as a prerequisite.
- Complex multi-agent orchestration.
- Microservices, message brokers, Kubernetes, or cloud hosting.
- A JavaScript-heavy frontend framework.

## 8. Delivery priorities

### First increment — local opportunity inbox

1. ASP.NET Core Razor Pages application targeting .NET 11.
2. Manual capture of source URL and original job-description text; employer/title optional.
3. Automatic capture timestamp.
4. EF Core 11 Code First model and SQLite persistence, with an initial migration.
5. Newest-first opportunity list and detail view.
6. Duplicate URL warning.

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

Evaluate the tool by whether it:

- Reduces time spent collecting and reviewing postings.
- Finds relevant opportunities that would otherwise be missed.
- Reduces duplicate review.
- Helps prioritize applications against real constraints.
- Prevents loss of application and follow-up details.
- Reveals recurring market skill requirements that can inform skills development.

Do not treat lines of code, number of integrations, model complexity, or number of captured listings as success by themselves.

## 10. Open decisions

Resolve these through a small working prototype rather than speculative design:

- Production deployment/update mechanism details (Windows Service, release layout, and migration execution procedure).
- Exact candidate-profile editing workflow.
- Which job-alert/email format is worth importing first.
- Whether local AI evaluation is sufficiently accurate and useful on real postings.
- Whether a source integration is worth maintaining compared with manual or alert-based capture.
