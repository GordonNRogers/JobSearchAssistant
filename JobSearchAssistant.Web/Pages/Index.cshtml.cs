
using JobSearchAssistant.Web.Data;
using JobSearchAssistant.Web.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace JobSearchAssistant.Web.Pages;

public class IndexModel(ApplicationDbContext db) : PageModel
{
    public IList<JobOpportunity> Opportunities { get; private set; } =
        new List<JobOpportunity>();

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Opportunities = await db.JobOpportunities
            .OrderByDescending(o => o.CapturedAtUtc)
            .ToListAsync(cancellationToken);
    }
}
