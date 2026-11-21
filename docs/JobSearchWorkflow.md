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
| Potential titles | Senior Software Engineer, Senior Software Developer, Senior .NET/C# Developer, Backend Engineer/Developer, Application Developer/Engineer, Software Engineer, Systems Engineer, Integration Engineer, API Developer, Technical Lead (individual-contributor roles only) |
| Work arrangement | Fully remote |
| Geographic eligibility | Remote roles open to candidates in the United States |
| Employment | Full-time preferred; contract-to-hire acceptable; strictly contract is lower priority |
| Relevant supporting technologies | SQL Server, Azure, APIs, backend services, messaging and system integration |
| Lower-priority / avoid | Management roles, primarily production-support roles, and frontend-heavy positions |

**Titles are discovery hints, not qualification criteria.** Do not restrict the search to titles containing "Senior" or ".NET." Consider titles such as Software Engineer/Developer, Backend Engineer/Developer, Application Developer/Engineer, Systems Engineer, Integration Engineer, API Developer, and individual-contributor Technical Lead roles when the actual responsibilities and required-skills list align. Conversely, a senior-sounding title is not enough to make a posting relevant.

Evaluate the posting's "laundry list" of required and preferred qualifications, responsibilities, and technologies. Look for substantive overlap with demonstrated experience, especially C#, .NET, SQL Server, backend/business application development, APIs, Azure, messaging, and integration. Distinguish must-have requirements from preferred qualifications and incidental technology mentions. A role should not be rejected merely because the title is unfamiliar, nor accepted because it contains a familiar title or keyword.

The current search baseline is not a claim that every listed supporting technology is equally strong or equally important. Review the complete requirements against the user's actual experience; do not assume a match from a keyword alone.

Do not assume a posting is remote merely because a search result says "remote." Confirm geographic eligibility and any required office visits, travel, or hybrid expectations in the posting.

No compensation floor or exact salary target is established in this workflow. Do not invent one; record compensation when a posting provides it and review it against the user's expectations.

## 3. Search families

Configure comparable searches on Indeed, Dice, Monster, and ZipRecruiter. Use Glassdoor recommendations as a separate comparison source. Apply the remote and United States eligibility filters separately where the site supports them.

Search syntax differs between job boards. Treat the following as keyword/title concepts and adapt them to each site's supported search syntax; do not assume every site accepts Boolean operators.

### A — Direct .NET / C# roles

- Potential titles: .NET Developer, C# Developer, Software Developer, Software Engineer, Application Developer, Senior or Lead variants.
- Keywords: C#, .NET, ASP.NET Core, .NET Core.
- Highest-priority search family, but do not rely on title matching alone.

### B — Backend and API development

- Potential titles: Backend Engineer/Developer, API Developer/Engineer, Services Engineer, Software Engineer.
- Keywords: C#, .NET, ASP.NET Core, REST APIs, web services, microservices where relevant.
- Include roles where backend implementation is central and .NET/C# is a substantial part of the required skills.

### C — Enterprise and business application development

- Potential titles: Application Developer/Engineer, Enterprise Application Developer, Business Systems Developer, Software Engineer, Systems Developer.
- Keywords: C#, .NET, SQL Server, Azure, APIs, integration, messaging, data access.
- Look for internal/business applications, data-intensive systems, workflow systems, and application integration.

### D — Systems, integration, and platform-adjacent development

- Potential titles: Integration Engineer/Developer, Systems Engineer/Developer, Platform Engineer, Technical Lead (individual contributor).
- Keywords: C#, .NET, SQL Server, Azure, service integration, messaging, ETL/data movement, authentication/authorization, enterprise systems.
- Inspect responsibilities carefully. Exclude infrastructure-only, operations-only, or support-only roles when application development is not a meaningful part of the work.

### E — Broad software engineering titles

- Potential titles: Software Engineer, Software Developer, Product Engineer, Senior Software Engineer, Senior Software Developer, Lead Software Engineer.
- Pair broad titles with C#/.NET and relevant supporting skills, or inspect the full requirements list.
- Purpose: catch relevant roles whose title does not identify the technology, without treating every software engineering vacancy as a match.

The same title can describe very different jobs. Search broadly enough to discover postings, then assess the actual responsibilities and required/preferred qualifications. Do not create a separate alert for every title variation; combine where the job board supports it and keep the total number of searches manageable.

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

## 8. Search interpretation

The user has confirmed that the title itself is not a meaningful filter. The actual responsibilities and the complete required/preferred skills list matter more. Keep title coverage broad and use the skills and duties to determine relevance.

Do not turn this into an indiscriminate search for every software role. The practical filter is whether the posting's core work and qualification list overlap materially with the user's demonstrated experience and target constraints.
