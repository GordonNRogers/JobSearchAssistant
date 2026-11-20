using JobSearchAssistant.Web.Data;
using JobSearchAssistant.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace JobSearchAssistant.Web.Pages.Opportunities;

public class DetailsModel(ApplicationDbContext db) : PageModel
{
    public JobOpportunity Opportunity { get; private set; } = null!;

    [BindProperty]
    public string? Comments { get; set; }

    public async Task<IActionResult> OnGetAsync(
        int id,
        CancellationToken cancellationToken)
    {
        if (!await LoadOpportunityAsync(id, cancellationToken))
        {
            return NotFound();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostSaveCommentsAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var opportunity = await db.JobOpportunities
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

        if (opportunity is null)
        {
            return NotFound();
        }

        opportunity.Comments = Comments?.Trim();

        await db.SaveChangesAsync(cancellationToken);

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostDeleteAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var opportunity = await db.JobOpportunities
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

        if (opportunity is null)
        {
            return NotFound();
        }

        db.JobOpportunities.Remove(opportunity);
        await db.SaveChangesAsync(cancellationToken);

        return RedirectToPage("/Index");
    }

    private async Task<bool> LoadOpportunityAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var opportunity = await db.JobOpportunities
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

        if (opportunity is null)
        {
            return false;
        }

        Opportunity = opportunity;
        Comments = opportunity.Comments;

        return true;
    }
}