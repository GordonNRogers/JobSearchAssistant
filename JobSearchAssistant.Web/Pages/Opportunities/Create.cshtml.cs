
using JobSearchAssistant.Web.Data;
using JobSearchAssistant.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace JobSearchAssistant.Web.Pages.Opportunities;

public class CreateModel(ApplicationDbContext db) : PageModel
{
    [BindProperty]
    public JobOpportunity Opportunity { get; set; } = new();

    [BindProperty]
    public bool ConfirmDuplicate { get; set; }

    public string? DuplicateWarning { get; private set; }

    public void OnGet()
    {
    }


    public async Task<IActionResult> OnPostAsync(
        CancellationToken cancellationToken)
    {
        Uri? sourceUri = null;

        var sourceUrl = Opportunity.SourceUrl?.Trim();

        if (string.IsNullOrWhiteSpace(sourceUrl) ||
            !Uri.TryCreate(sourceUrl, UriKind.Absolute, out sourceUri) ||
            (sourceUri.Scheme != Uri.UriSchemeHttp &&
             sourceUri.Scheme != Uri.UriSchemeHttps))
        {
            ModelState.AddModelError(
                "Opportunity.SourceUrl",
                "Enter a valid HTTP or HTTPS URL.");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var normalizedUrl = NormalizeUrl(sourceUri!);

        var existingUrls = await db.JobOpportunities
            .Select(o => o.SourceUrl)
            .ToListAsync(cancellationToken);

        var duplicateExists = existingUrls.Any(existingUrl =>
        {
            if (!Uri.TryCreate(existingUrl, UriKind.Absolute, out var existingUri) ||
                existingUri is null)
            {
                return false;
            }

            return NormalizeUrl(existingUri) == normalizedUrl;
        });

        if (duplicateExists && !ConfirmDuplicate)
        {
            DuplicateWarning =
                "An opportunity with an equivalent source URL already exists. " +
                "Review the existing opportunity before deciding whether to save another.";

            return Page();
        }

        Opportunity.SourceUrl = sourceUrl!;
        Opportunity.CapturedAtUtc = DateTime.UtcNow;

        db.JobOpportunities.Add(Opportunity);
        await db.SaveChangesAsync(cancellationToken);

        return RedirectToPage("/Index");
    }

    private static string NormalizeUrl(Uri uri)
    {
        var builder = new UriBuilder(uri)
        {
            Fragment = string.Empty
        };

        var path = builder.Path.TrimEnd('/');
        builder.Path = string.IsNullOrEmpty(path) ? "/" : path;

        return builder.Uri
            .GetComponents(UriComponents.HttpRequestUrl, UriFormat.UriEscaped);
    }
}
