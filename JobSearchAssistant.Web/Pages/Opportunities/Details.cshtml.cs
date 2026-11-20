
using JobSearchAssistant.Web.Data;
using JobSearchAssistant.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace JobSearchAssistant.Web.Pages.Opportunities;

public class DetailsModel(ApplicationDbContext db) : PageModel
{
    public JobOpportunity Opportunity { get; private set; } = null!;

    public async Task<IActionResult> OnGetAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var opportunity = await db.JobOpportunities
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

        if (opportunity is null)
        {
            return NotFound();
        }

        Opportunity = opportunity;
        return Page();
    }
}
