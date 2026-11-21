# Glassdoor HTTP Scraping Experiment

**Status:** Initial anonymous HTTP investigation completed; Cloudflare challenge blocks retrieval; no extraction demonstrated  
**Branch:** `experiment/glassdoor-http-scraping`  
**Target:** User-provided Glassdoor Recruiter job-view URL

## Purpose

Determine whether JobSearchAssistant can retrieve and extract job details from the supplied Glassdoor job-view page using ordinary HTTP requests. This is a contained experiment, not a commitment to add production job-board scraping.

The supplied URL opens in the user's normal browser. That browser may already have an authenticated Glassdoor session, so browser visibility does not establish anonymous accessibility.

Do not copy the full test URL into repository documentation. Its query string contains session- or tracking-like values that should not be treated as harmless public identifiers.

## Agreed technical direction

- Use `curl.exe` from PowerShell for request/response diagnostics.
- Use .NET `HttpClient` for any application implementation. Do not shell out to curl at runtime.
- Introduce a small `IWebScraper` abstraction for job retrieval and extraction.
- Start with a Glassdoor-specific implementation if the HTTP investigation demonstrates a viable and permitted approach.
- Expect site-specific retrieval strategies; do not build a generic scraping framework or plugin marketplace prematurely.
- Keep HTTP session/authentication management separate from page parsing and the `IWebScraper` contract.

## Investigation sequence

1. Make an anonymous GET request to the supplied URL.
2. Capture:
   - Initial and final HTTP status.
   - Redirect chain and final response URI.
   - Relevant response headers.
   - Content type and response size.
   - Response body/HTML for local inspection.
3. Determine whether the body contains the job title, employer, description, and other relevant fields.
4. Determine whether the response is instead a login page, access challenge, or JavaScript-rendered shell.
5. If authentication is required, investigate how the existing browser session works and what minimum session mechanism is appropriate.
6. Reproduce the viable request using .NET `HttpClient`.
7. Implement extraction behind `IWebScraper` and test against captured, sanitized fixtures and live behavior where appropriate.

Example anonymous diagnostic command (replace the placeholder locally; do not commit the URL or authenticated artifacts):

```powershell
$url = "PASTE_TEST_URL_LOCALLY"

curl.exe -L `
    -D response-headers.txt `
    -o response.html `
    -w "`nHTTP: %{http_code}`nFinal URL: %{url_effective}`nContent Type: %{content_type}`n" `
    "$url"
```

The command follows redirects and saves response headers and body. If the URL contains sensitive or account-specific query values, avoid sharing the command or output unredacted.

## Authentication and session considerations

Authentication is unresolved. Do not assume a username/password login flow is necessary or appropriate.

Potential concerns to investigate only if evidence requires them:

- Cookies and session identifiers.
- Cookie expiration and session renewal.
- Redirects between Glassdoor pages or identity providers.
- CSRF tokens or other request-specific state.
- Authentication challenges and access restrictions.
- Persistence of session state across application restarts.

Cookies, credentials, and session identifiers are secrets. Never put them in source control, PowerShell command history, logs, plaintext configuration, or shared diagnostic artifacts. If persistent cookie storage proves necessary, evaluate Windows-protected storage (such as DPAPI) before implementing it.

Ordinary redirects and cookie handling may be supported by `HttpClientHandler` and `CookieContainer`, but the actual response must guide configuration. Do not attempt to bypass CAPTCHA, bot challenges, or other access controls.

## Initial architectural sketch

- `IWebScraper`: application-facing contract for retrieving and extracting job information from a URL.
- Glassdoor scraper: site-specific parsing and request behavior.
- Scraper selection: choose the implementation appropriate to the target site/URL; keep the first version simple.
- HTTP transport/session component: shared request execution and, if justified, cookie/session state. Do not mix secret storage into page parsing.

Exact method signatures, result models, scraper selection, and session abstractions are intentionally deferred until the first response has been inspected.

## Exit criteria

The experiment succeeds only when a permitted, repeatable .NET `HttpClient` request retrieves the target job data and a Glassdoor scraper extracts the agreed fields with tests covering observed response variants.

If Glassdoor requires unsupported or prohibited access, or if retrieval is not reliable enough to maintain, document the limitation and stop. Do not escalate into browser automation or challenge circumvention merely to force a successful result.

## Current evidence

The user confirms that the supplied URL displays in their normal browser. That browser may have an authenticated Glassdoor session; browser visibility does not establish that an independent HTTP client can retrieve the page.

Two anonymous request paths have now been tested:

1. A PowerShell command using `curl` invoked the PowerShell `Invoke-WebRequest` alias rather than native curl. Its response body was a Glassdoor security interstitial containing "Humans only" and "Enable JavaScript and cookies to continue", with Cloudflare managed-challenge markers.
2. A subsequent request used native `curl.exe` with redirect following, response-header capture, and body capture. It returned:
   - HTTP status: `403 Forbidden`
   - Final URL: the requested Glassdoor Recruiter job-view URL (no redirect to another page was observed in the reported output).
   - Content type: `text/html; charset=UTF-8`
   - Response body size: 13,143 bytes.
   - Headers included `Server: cloudflare` and `Cf-Mitigated: challenge`; a `CF-RAY` identifier was also present.
   - HTML title: `Security | Glassdoor`.
   - Cloudflare challenge markers were present; login markers were absent.

The response is a Cloudflare challenge page, not the requested job posting and not an ordinary Glassdoor login page. A separate string check found job-listing-related terms in the HTML, but those terms can occur in challenge markup or embedded scripts and do not demonstrate that job data was returned.

### Assessment

- Anonymous retrieval of this URL is currently blocked by Cloudflare under the tested conditions, using both PowerShell's `Invoke-WebRequest` alias and native `curl.exe`.
- This is evidence of an access challenge, not evidence that the URL is invalid or that the browser cannot display the job.
- Authentication and automated-request access are separate concerns. We have not established whether the browser's authenticated session changes the response, nor whether an authorized integration or data-access route exists.
- Do not attempt to evade or bypass Cloudflare challenges, CAPTCHA, or other access controls. Do not experiment with spoofed headers, browser fingerprinting, or copied browser cookies as a way to defeat the challenge.
- Do not commit the raw response body, full test URL, public IP address, Cloudflare Ray ID, challenge tokens, cookies, or other request-specific artifacts. The diagnostic output shared during the investigation included identifying/request-specific values; they are intentionally omitted here.

No application implementation, build, or test has been performed for this experiment. The next step is a scope decision: investigate an authorized Glassdoor data-access option or treat Glassdoor as a manual-capture source. Do not proceed to a Glassdoor `HttpClient` scraper unless a permitted, repeatable retrieval path is established.
