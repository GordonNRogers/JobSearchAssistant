
using System.ComponentModel.DataAnnotations;

namespace JobSearchAssistant.Web.Models;

public class JobOpportunity
{
    public int Id { get; set; }

    [Required(ErrorMessage = "A source URL is required.")]
    [Url(ErrorMessage = "Enter a valid URL.")]
    public string SourceUrl { get; set; } = string.Empty;

    public string? CompanyName { get; set; }

    public string? JobTitle { get; set; }

    public string? Comments { get; set; }

    [Required(ErrorMessage = "The original job posting text is required.")]
    public string OriginalPostingText { get; set; } = string.Empty;

    public DateTime CapturedAtUtc { get; set; }

}
