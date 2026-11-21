# Glassdoor HTTP Scraping Experiment

**Status:** Closed — no-go for automated Glassdoor retrieval without express written authorization  
**Last updated:** 2026-11-21  
**Branch:** `experiment/glassdoor-http-scraping`  
**Target:** User-provided Glassdoor Recruiter job-view URL

## Purpose

Determine whether JobSearchAssistant could retrieve and extract job details from the supplied Glassdoor job-view page using ordinary HTTP requests. This was a contained feasibility experiment, not a commitment to add production job-board scraping.

The supplied URL opens in the user's normal browser. That browser may already have an authenticated Glassdoor session; browser visibility does not establish anonymous HTTP accessibility or permission for automated collection.

Do not copy the full test URL into repository documentation. Its query string contains session- or tracking-like values that should not be treated as harmless public identifiers.

## Initial technical hypothesis

The initial hypothesis was to:

- Use `curl.exe` from PowerShell for HTTP diagnostics only.
- Use .NET `HttpClient` for any application implementation; do not shell out to curl at runtime.
- Consider a small `IWebScraper` contract and a Glassdoor-specific implementation only if retrieval proved both technically viable and permitted.
- Keep session/authentication management separate from page parsing.
- Avoid a generic scraping framework or plugin marketplace.

This was a hypothesis, not an implementation decision. The investigation did not reach the point where an `IWebScraper` or Glassdoor scraper should be built.

## HTTP investigation and evidence

The user confirms that the supplied URL displays in their normal browser. That browser may have an authenticated Glassdoor session.

Two anonymous request paths were tested:

1. A PowerShell command using `curl` invoked the PowerShell `Invoke-WebRequest` alias rather than native curl. Its response body was a Glassdoor security interstitial containing "Humans only" and "Enable JavaScript and cookies to continue", with Cloudflare managed-challenge markers.
2. A subsequent request used native `curl.exe` with redirect following, response-header capture, and body capture. It returned:
   - HTTP status: `403 Forbidden`
   - Final URL: the requested Glassdoor Recruiter job-view URL; no redirect to another page was reported.
   - Content type: `text/html; charset=UTF-8`
   - Response body size: 13,143 bytes.
   - Headers included `Server: cloudflare` and `Cf-Mitigated: challenge`; a `CF-RAY` identifier was also present.
   - HTML title: `Security | Glassdoor`.
   - Cloudflare challenge markers were present; login markers were absent.

The response is a Cloudflare challenge page, not the requested job posting and not an ordinary Glassdoor login page. A separate string check found job-listing-related terms in the HTML, but those terms can occur in challenge markup or embedded scripts and do not demonstrate that job data was returned.

### HTTP conclusion

Anonymous retrieval of this URL was blocked by Cloudflare under the tested conditions, using both PowerShell's `Invoke-WebRequest` alias and native `curl.exe`.

This is evidence of an access challenge, not evidence that the URL is invalid or that the browser cannot display the job. We did not test authenticated browser-session replay, and will not attempt to defeat the challenge by copying cookies, spoofing browser fingerprints, changing headers to evade detection, or using challenge-solving services.

## Terms and API research

The official [Glassdoor Terms of Use](https://www.glassdoor.com/about/terms/) identify Indeed, Inc. as the provider and state that the current terms were revised July 1, 2026.

Section 4.3, item 16, prohibits introducing software or automated agents to Glassdoor, or accessing the service to scrape, strip, or mine data, without express written permission. Items 22–24 also prohibit interfering with or disrupting the service and attempting to circumvent a security feature. Section 3.2 separately limits use of Glassdoor content to personal use in connection with the services and restricts reproduction and other uses of content not supplied by the user.

Glassdoor's [Help Center article titled "Glassdoor API"](https://help.glassdoor.com/s/article/Glassdoor-API?language=en_US) is reported by current 2026 secondary sources as stating that Glassdoor no longer supports API partnerships (article updated November 26, 2024). The Help Center page did not render its article body in the research browser, so that API-program statement is recorded as secondary-source reporting rather than independently verified page text. The official current Terms of Use are directly accessible and independently establish the express-written-permission requirement for automated scraping/mining.

The existence of historical Glassdoor API documentation or API terms does not establish that a new developer can obtain access today. No current, self-service public API or authorized feed for retrieving this user's saved Recruiter job listings was identified during this investigation. No evidence was found that a Recruiter account's saved-job view grants a separate right to automated extraction.

Sources checked:

- [Glassdoor Terms of Use — revised July 1, 2026](https://www.glassdoor.com/about/terms/)
- [Glassdoor Terms History](https://www.glassdoor.com/about/terms-history/)
- [Glassdoor Help Center — Glassdoor API](https://help.glassdoor.com/s/article/Glassdoor-API?language=en_US) (article body did not render in the research browser)
- [Headcount Labs: Does Glassdoor have an API?](https://headcountlabs.com/guides/glassdoor-api/) (secondary source reporting the Help Center's API-partnership status)

This is an engineering scope decision based on the published terms and available access evidence, not legal advice.

## Decision

**Do not implement a Glassdoor HTTP scraper, browser automation, challenge bypass, or automated extraction of the user's saved Recruiter listings.**

Reasons:

1. The tested anonymous HTTP requests receive a Cloudflare managed challenge and return no job data.
2. Glassdoor's current terms expressly require written permission for automated scraping/mining and prohibit circumvention of security features.
3. No currently available, documented self-service API or feed for this use case was identified.
4. Building around the challenge would add fragility and would not resolve the permission issue.

Reopen this experiment only if Glassdoor provides express written authorization covering the intended automated access and data use, or provides a documented integration/API that expressly supports this use case. Any such permission must be reviewed before implementation.

## Practical alternative for JobSearchAssistant

Keep the existing manual opportunity-capture workflow. For a Glassdoor-discovered role:

- Use the Glassdoor page manually to review the opportunity.
- Prefer the employer's own career page or its ATS-hosted posting as the source URL and source text when that posting is available.
- Otherwise, retain the Glassdoor URL as a reference and enter the job details and personal notes needed for the user's own tracking. Do not build automated collection around the Glassdoor page.
- Treat copying the complete Glassdoor posting into a separate application as a distinct content-use question; the terms contain restrictions beyond automated scraping. If retaining the complete Glassdoor text is important, obtain clarification/permission rather than assuming that manual copying resolves every terms issue.

This alternative does not require new scraping infrastructure or a change to the application's architecture.

## Security and artifact handling

Cookies, credentials, session identifiers, Cloudflare Ray IDs, challenge tokens, and request-specific values are sensitive or unnecessary diagnostic artifacts. Do not put them in source control, PowerShell command history, logs, plaintext configuration, or shared diagnostic files.

Do not commit the raw response body, full test URL, public IP address, Cloudflare Ray ID, challenge tokens, cookies, or other request-specific artifacts. The diagnostic output shared during the investigation included identifying/request-specific values; they are intentionally omitted here.

## Work performed and verification

- Documentation-only experiment; no application code was changed.
- No `IWebScraper`, Glassdoor scraper, session manager, or browser automation was implemented.
- No application build or tests were run as part of this experiment.
- The HTTP findings above are based on the user's reported command output.
- The current Terms of Use were reviewed directly; the API-partnership status is attributed to secondary reporting because the Help Center article body did not render during research.

**Outcome:** Close the Glassdoor scraping experiment as no-go under current evidence and terms. Continue using the existing manual capture capability; prefer employer-hosted job postings where available.


## Follow-up design question — browser-assisted manual capture

A browser extension was considered as a way to reduce the friction of manually capturing a Glassdoor-discovered opportunity. This is distinct from reopening the HTTP scraping experiment.

### Conservative option: local capture popup or side panel

A future extension could provide a small popup or side panel while the user remains on the Glassdoor tab. After an explicit click, it could read the active tab URL and populate the source URL field. The user would manually enter employer, title, location, and personal notes, then submit a draft to a loopback-only JobSearchAssistant intake endpoint. This avoids navigating to a separate application page and does not require the extension to read Glassdoor page content.

This still needs a security review of browser permissions, local-origin communication, request-forgery protections, and the local endpoint. The extension and endpoint are not implemented.

### Manual text transfer option

The user could select and copy relevant text in the browser, open the extension popup, and paste it into a text field. This reduces context switching, but it remains copying Glassdoor content into another application. Glassdoor's terms contain content-use restrictions separate from the prohibition on automated scraping; manual copying does not automatically resolve those restrictions.

### Do not assume click-to-read selection is permitted

An extension that reads selected text from the page or receives selected text from a context-menu action after the user highlights it is more user-directed than background collection. However, the extension is still programmatically reading and transferring website content. Glassdoor's current terms do not provide an explicit exception for this design. Do not treat a user click, a manually highlighted selection, or use of Chrome's activeTab permission as Glassdoor authorization. Obtain express written permission or clarification before implementing it for Glassdoor.

A safe engineering boundary absent that permission is therefore: read only the active tab URL after a user gesture; have the user manually enter the fields; do not read page DOM, selected text, or clipboard contents automatically. If the user manually pastes text, retain the separate content-use caveat above.

### Proposed intake payload (not implemented)

- Source URL (read from active tab after explicit user action)
- Employer and job title (user-entered)
- Location (user-entered, optional)
- Personal notes (user-entered, optional)
- Posting text (optional, only when the user supplies it and its reuse is appropriate)
- Capture timestamp (assigned by JobSearchAssistant)

The endpoint should create a reviewable draft, not silently save an opportunity. This proposal does not change the experiment's no-go decision for automated Glassdoor retrieval.
