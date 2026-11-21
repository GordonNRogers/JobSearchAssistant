# Job Discovery Workflow

**Status:** Working search baseline; validate against real results before building discovery automation  
**Last updated:** 2026-11-21

## 1. Purpose

Improve the number and quality of relevant opportunities found each week before investing in automated collection or AI evaluation.

The current job search is still in its foundation phase. Resume and profile setup has taken priority, and the number of opportunities found so far is limited. Glassdoor recommendations have so far produced a better apparent fit than the other job boards, particularly for .NET, senior-level, and remote positions. This is an early qualitative observation, not yet a measured comparison.

Use Glassdoor as a current discovery benchmark, not as an automated data source. Keep the job search active independently of JobSearchAssistant development.

## 2. Target opportunity profile

Use this as the initial search baseline. Adjust it when actual results show that a criterion is too restrictive or too broad.

| Dimension | Initial target |
|---|---|
| Primary technology | C#, .NET, ASP.NET Core |
| Seniority | Senior Software Developer, Senior Software Engineer, Senior .NET Developer, Senior C# Developer |
| Work arrangement | Fully remote |
| Geographic eligibility | Remote roles open to candidates in the United States |
| Employment | Full-time preferred; contract-to-hire acceptable; strictly contract is lower priority |
| Relevant supporting technologies | SQL Server, Azure, APIs, backend services, messaging and system integration |
| Lower-priority / avoid | Management roles, primarily production-support roles, and frontend-heavy positions |

Do not require .NET to appear in the job title. A Senior Software Engineer role can be relevant when C#/.NET is a primary implementation technology in the description.

Do not assume a posting is remote merely because a search result says "remote." Confirm geographic eligibility and any required office visits, travel, or hybrid expectations in the posting.

No compensation floor or exact salary target is established in this workflow. Do not invent one; record compensation when a posting provides it and review it against the user's expectations.

## 3. Search families

Configure comparable searches on Indeed, Dice, Monster, and ZipRecruiter. Use Glassdoor recommendations as a separate comparison source. Apply the remote and United States eligibility filters separately where the site supports them.

Search syntax differs between job boards. Treat the following as keyword/title concepts and adapt them to each site's supported search syntax; do not assume every site accepts Boolean operators.

### A — Direct .NET roles

- Titles: Senior .NET Developer, Senior C# Developer, Senior Software Developer.
- Keywords: C#, .NET, ASP.NET Core, .NET Core.
- Highest-priority search family.

### B — Backend software engineering

- Titles/phrases: Senior Backend Engineer, Senior Backend Developer, Senior Software Engineer.
- Keywords: C#, .NET, ASP.NET Core.
- Include only when .NET/C# is a primary technology, not a minor or incidental requirement.

### C — Enterprise application development

- Keywords: C#, .NET, SQL Server, Azure, APIs, integration, messaging.
- Look for business applications, data-intensive systems, backend services, and application integration.
- Do not let a long list of optional technologies obscure the requirement that C#/.NET be central to the role.

### D — Broader senior software engineering

- Titles: Senior Software Engineer, Senior Software Developer.
- Pair the title search with C#/.NET keywords or inspect the technology requirements carefully.
- Purpose: catch relevant roles whose titles do not explicitly mention .NET without turning the search into an unrestricted software-engineering search.

Avoid creating many nearly identical alerts. Start with these four search families and refine them from observed results.

## 4. Sources and collection boundaries

- **Glassdoor:** Current qualitative benchmark because its recommendations have appeared better matched. Review recommendations manually. Do not automate retrieval or extract Glassdoor page content.
- **Indeed, Dice, Monster, ZipRecruiter:** Configure comparable searches and daily alerts where available. Compare actual relevance rather than raw result volume.
- **Employer career pages and ATS-hosted postings:** Prefer these as the source URL and source text when a promising listing is available there.
- **Email alerts and user-provided exports:** Potential future intake sources. Evaluate their actual formats before designing an importer.

The Glassdoor HTTP retrieval experiment is closed as no-go for automated retrieval without express written authorization. Do not use scraping, browser automation, DOM extraction, selected-text transfer, clipboard monitoring, cookie replay, challenge bypass, or background collection as a workaround. See [Glassdoor HTTP Retrieval Experiment](experiments/GlassdoorHttpScraping.md).

The current JobSearchAssistant inbox requires the source URL and original posting text. If a Glassdoor recommendation cannot be followed to an employer/ATS posting, do not assume the application currently supports URL-only capture. Keep the reference in the normal manual review process rather than changing the application's capture contract prematurely.

## 5. Review routine

Keep the routine bounded so the job search does not become a full-day browsing exercise.

Suggested daily allocation: approximately 20–30 minutes, adjusted to the volume of alerts.

1. Review Glassdoor recommendations.
2. Review new alerts from the other job boards.
3. Open promising listings and verify technology, seniority, remote eligibility, and employment type.
4. Capture only opportunities worth a closer look. Prefer the employer/ATS posting as the source when available.
5. Continue directly to an application when an opportunity is clearly worth pursuing; do not let data entry delay an application.

This routine is a starting point, not a quota. Application activity, networking, and interview preparation take precedence over maintaining the tool.

## 6. Two-week source comparison

For an initial two-week period, keep a simple tally by source. Do not build reporting features for this experiment.

Track:

- Number of new postings surfaced.
- Number that are genuinely relevant to the target profile.
- Number confirmed as fully remote and eligible for a U.S.-based applicant.
- Number with C#/.NET as a primary technology.
- Number at the desired seniority level.
- Number worth applying to.
- Notable mismatch reasons (for example: hybrid, wrong geography, support-heavy, frontend-heavy, or .NET only incidental).

Use consistent definitions across sources. A posting can satisfy multiple dimensions; "relevant" means it is a plausible fit overall, not merely that it contains the keyword .NET.

At the end of the period, compare quality as well as volume. A source with fewer results but a higher proportion of plausible opportunities may be more useful. Glassdoor is currently the qualitative benchmark, but the tally should be allowed to confirm or overturn that impression.

## 7. Implications for JobSearchAssistant

Do not build an automated Glassdoor integration. Do not prioritize AI evaluation while the main bottleneck is finding enough suitable postings.

After the source comparison, use observed friction to choose the next small increment. Likely candidates are:

1. Record source/provenance consistently.
2. Improve cross-source duplicate detection.
3. Add deterministic filtering for technology, title/seniority, work arrangement, geography, and employment type, once those fields can be captured reliably.
4. Investigate one legitimate alert/import source at a time.
5. Consider AI-assisted evaluation only if reviewing real postings becomes a demonstrated bottleneck after discovery improves.

The existing manual inbox remains useful. Do not add fields, importers, or automation solely because they appear on this list. Confirm the recurring need first.

## 8. Open question

Confirm whether the search should include Senior Software Engineer / Senior Software Developer postings whenever C#/.NET is a primary technology, even if .NET is not in the title. The search plan recommends including them to avoid missing relevant roles, but the user's explicit preference on this boundary has not yet been recorded.
