
using System.Net;
using System.Text.RegularExpressions;
using JobSearchAssistant.Web.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace JobSearchAssistant.Tests;

public class OpportunityInboxTests
{
    [Fact]
    public async Task Index_WhenNoOpportunitiesExist_DisplaysEmptyState()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        await factory.InitializeDatabaseAsync();

        var response = await client.GetAsync("/");

        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync();

        Assert.Contains("Opportunity Inbox", html);
        Assert.Contains("No opportunities have been captured yet.", html);
        Assert.Contains("Capture Opportunity", html);
    }

    [Fact]
    public async Task Create_WithValidOpportunity_SavesAndRedirectsToInbox()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

        await factory.InitializeDatabaseAsync();

        var pageResponse = await client.GetAsync("/Opportunities/Create");
        pageResponse.EnsureSuccessStatusCode();

        var pageHtml = await pageResponse.Content.ReadAsStringAsync();
        var token = ExtractAntiforgeryToken(pageHtml);

        var capturedBefore = DateTime.UtcNow;

        var form = new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Opportunity.SourceUrl"] = "https://example.com/jobs/software-developer",
            ["Opportunity.CompanyName"] = "Example Company",
            ["Opportunity.JobTitle"] = "Senior Software Developer",
            ["Opportunity.OriginalPostingText"] =
                "This is the complete original job posting."
        };

        var response = await client.PostAsync(
            "/Opportunities/Create",
            new FormUrlEncodedContent(form));

        var capturedAfter = DateTime.UtcNow;

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/", response.Headers.Location?.OriginalString);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var saved = await db.JobOpportunities.SingleAsync();

        Assert.Equal(
            "https://example.com/jobs/software-developer",
            saved.SourceUrl);
        Assert.Equal("Example Company", saved.CompanyName);
        Assert.Equal("Senior Software Developer", saved.JobTitle);
        Assert.Equal(
            "This is the complete original job posting.",
            saved.OriginalPostingText);

        Assert.InRange(
            saved.CapturedAtUtc,
            capturedBefore,
            capturedAfter);
    }

    private static string ExtractAntiforgeryToken(string html)
    {
        var match = Regex.Match(
            html,
            """<input\b(?=[^>]*\bname="__RequestVerificationToken")(?=[^>]*\bvalue="([^"]+)")[^>]*>""");

        Assert.True(match.Success, "Antiforgery token was not found in the page.");

        return match.Groups[1].Value;
    }

    [Fact]
    public async Task Create_WithInvalidUrl_DoesNotSaveOpportunity()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        await factory.InitializeDatabaseAsync();

        var pageResponse = await client.GetAsync("/Opportunities/Create");
        pageResponse.EnsureSuccessStatusCode();

        var pageHtml = await pageResponse.Content.ReadAsStringAsync();
        var token = ExtractAntiforgeryToken(pageHtml);

        var form = new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Opportunity.SourceUrl"] = "not-a-valid-url",
            ["Opportunity.CompanyName"] = "Example Company",
            ["Opportunity.JobTitle"] = "Senior Software Developer",
            ["Opportunity.OriginalPostingText"] =
                "This is the complete original job posting."
        };

        var response = await client.PostAsync(
            "/Opportunities/Create",
            new FormUrlEncodedContent(form));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();

        Assert.Contains("Enter a valid URL.", html);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        Assert.Equal(0, await db.JobOpportunities.CountAsync());
    }

    [Fact]
    public async Task Create_WithMissingPostingText_DoesNotSaveOpportunity()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        await factory.InitializeDatabaseAsync();

        var pageResponse = await client.GetAsync("/Opportunities/Create");
        pageResponse.EnsureSuccessStatusCode();

        var pageHtml = await pageResponse.Content.ReadAsStringAsync();
        var token = ExtractAntiforgeryToken(pageHtml);

        var form = new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Opportunity.SourceUrl"] = "https://example.com/jobs/developer",
            ["Opportunity.CompanyName"] = "Example Company",
            ["Opportunity.JobTitle"] = "Software Developer",
            ["Opportunity.OriginalPostingText"] = ""
        };

        var response = await client.PostAsync(
            "/Opportunities/Create",
            new FormUrlEncodedContent(form));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();

        Assert.Contains(
            "The original job posting text is required.",
            html);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        Assert.Equal(0, await db.JobOpportunities.CountAsync());
    }

    [Fact]
    public async Task Create_WithDuplicateUrl_DisplaysWarningAndDoesNotSave()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        await factory.InitializeDatabaseAsync();

        // Save the original opportunity.
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            db.JobOpportunities.Add(new JobSearchAssistant.Web.Models.JobOpportunity
            {
                SourceUrl = "https://example.com/jobs/developer",
                CompanyName = "Example Company",
                JobTitle = "Software Developer",
                OriginalPostingText = "Original posting",
                CapturedAtUtc = DateTime.UtcNow
            });

            await db.SaveChangesAsync();
        }

        var pageResponse = await client.GetAsync("/Opportunities/Create");
        pageResponse.EnsureSuccessStatusCode();

        var pageHtml = await pageResponse.Content.ReadAsStringAsync();
        var token = ExtractAntiforgeryToken(pageHtml);

        var form = new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Opportunity.SourceUrl"] = "https://example.com/jobs/developer/",
            ["Opportunity.CompanyName"] = "Example Company",
            ["Opportunity.JobTitle"] = "Software Developer",
            ["Opportunity.OriginalPostingText"] = "Duplicate posting"
        };

        var response = await client.PostAsync(
            "/Opportunities/Create",
            new FormUrlEncodedContent(form));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();

        Assert.Contains("Possible duplicate.", html);
        Assert.Contains("Save Anyway", html);

        using var verificationScope = factory.Services.CreateScope();
        var verificationDb = verificationScope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        Assert.Equal(1, await verificationDb.JobOpportunities.CountAsync());
    }

    [Fact]
    public async Task Create_WithDuplicateUrlAndConfirmation_SavesOpportunity()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

        await factory.InitializeDatabaseAsync();

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            db.JobOpportunities.Add(new JobSearchAssistant.Web.Models.JobOpportunity
            {
                SourceUrl = "https://example.com/jobs/developer",
                CompanyName = "Example Company",
                JobTitle = "Software Developer",
                OriginalPostingText = "Original posting",
                CapturedAtUtc = DateTime.UtcNow
            });

            await db.SaveChangesAsync();
        }

        var pageResponse = await client.GetAsync("/Opportunities/Create");
        pageResponse.EnsureSuccessStatusCode();

        var pageHtml = await pageResponse.Content.ReadAsStringAsync();
        var token = ExtractAntiforgeryToken(pageHtml);

        var form = new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Opportunity.SourceUrl"] = "https://example.com/jobs/developer/",
            ["Opportunity.CompanyName"] = "Example Company",
            ["Opportunity.JobTitle"] = "Software Developer",
            ["Opportunity.OriginalPostingText"] = "Confirmed duplicate posting",
            ["ConfirmDuplicate"] = "true"
        };

        var response = await client.PostAsync(
            "/Opportunities/Create",
            new FormUrlEncodedContent(form));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/", response.Headers.Location?.OriginalString);

        using var verificationScope = factory.Services.CreateScope();
        var verificationDb = verificationScope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        Assert.Equal(2, await verificationDb.JobOpportunities.CountAsync());

        Assert.Contains(
            await verificationDb.JobOpportunities.ToListAsync(),
            opportunity => opportunity.OriginalPostingText ==
                "Confirmed duplicate posting");
    }

    [Fact]
    public async Task Create_WithDifferentUrlPathCasing_IsNotTreatedAsDuplicate()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

        await factory.InitializeDatabaseAsync();

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            db.JobOpportunities.Add(new JobSearchAssistant.Web.Models.JobOpportunity
            {
                SourceUrl = "https://example.com/Jobs/Developer",
                OriginalPostingText = "Original posting",
                CapturedAtUtc = DateTime.UtcNow
            });

            await db.SaveChangesAsync();
        }

        var pageResponse = await client.GetAsync("/Opportunities/Create");
        pageResponse.EnsureSuccessStatusCode();

        var pageHtml = await pageResponse.Content.ReadAsStringAsync();
        var token = ExtractAntiforgeryToken(pageHtml);

        var form = new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Opportunity.SourceUrl"] = "https://example.com/jobs/developer",
            ["Opportunity.OriginalPostingText"] = "Different posting"
        };

        var response = await client.PostAsync(
            "/Opportunities/Create",
            new FormUrlEncodedContent(form));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        using var verificationScope = factory.Services.CreateScope();
        var verificationDb = verificationScope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        Assert.Equal(2, await verificationDb.JobOpportunities.CountAsync());
    }

    [Fact]
    public async Task Details_WithExistingOpportunity_DisplaysCompleteOpportunity()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        await factory.InitializeDatabaseAsync();

        int opportunityId;

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var opportunity = new JobSearchAssistant.Web.Models.JobOpportunity
            {
                SourceUrl = "https://example.com/jobs/senior-developer",
                CompanyName = "Example Company",
                JobTitle = "Senior Developer",
                OriginalPostingText =
                    "Full posting text.\nResponsibilities include application development.",
                CapturedAtUtc = DateTime.UtcNow
            };

            db.JobOpportunities.Add(opportunity);
            await db.SaveChangesAsync();

            opportunityId = opportunity.Id;
        }

        var response = await client.GetAsync(
            $"/Opportunities/Details/{opportunityId}");

        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync();

        Assert.Contains("Example Company", html);
        Assert.Contains("Senior Developer", html);
        Assert.Contains("https://example.com/jobs/senior-developer", html);
        Assert.Contains("Full posting text.", html);
        Assert.Contains("Responsibilities include application development.", html);
    }

    [Fact]
    public async Task Index_WithMultipleOpportunities_DisplaysNewestFirst()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        await factory.InitializeDatabaseAsync();

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var now = DateTime.UtcNow;

            db.JobOpportunities.AddRange(
                new JobSearchAssistant.Web.Models.JobOpportunity
                {
                    SourceUrl = "https://example.com/jobs/older",
                    CompanyName = "Older Company",
                    JobTitle = "Older Job",
                    OriginalPostingText = "Older posting",
                    CapturedAtUtc = now.AddMinutes(-10)
                },
                new JobSearchAssistant.Web.Models.JobOpportunity
                {
                    SourceUrl = "https://example.com/jobs/newer",
                    CompanyName = "Newer Company",
                    JobTitle = "Newer Job",
                    OriginalPostingText = "Newer posting",
                    CapturedAtUtc = now
                });

            await db.SaveChangesAsync();
        }

        var response = await client.GetAsync("/");
        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync();

        var newerPosition = html.IndexOf("Newer Job", StringComparison.Ordinal);
        var olderPosition = html.IndexOf("Older Job", StringComparison.Ordinal);

        Assert.True(newerPosition >= 0, "Newer opportunity was not displayed.");
        Assert.True(olderPosition >= 0, "Older opportunity was not displayed.");
        Assert.True(newerPosition < olderPosition,
            "The inbox should display the newest opportunity first.");
    }

    [Fact]
    public async Task Details_SaveComments_PersistsTrimmedComments()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

        await factory.InitializeDatabaseAsync();

        int opportunityId;

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var opportunity = new JobSearchAssistant.Web.Models.JobOpportunity
            {
                SourceUrl = "https://example.com/jobs/developer",
                OriginalPostingText = "Original posting text",
                CapturedAtUtc = DateTime.UtcNow
            };

            db.JobOpportunities.Add(opportunity);
            await db.SaveChangesAsync();

            opportunityId = opportunity.Id;
        }

        var pageResponse = await client.GetAsync(
            $"/Opportunities/Details/{opportunityId}");

        pageResponse.EnsureSuccessStatusCode();

        var html = await pageResponse.Content.ReadAsStringAsync();
        var token = ExtractAntiforgeryToken(html);

        var form = new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Comments"] = "  Follow up with recruiter next week.  "
        };

        var response = await client.PostAsync(
            $"/Opportunities/Details/{opportunityId}?handler=SaveComments",
            new FormUrlEncodedContent(form));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(
            $"/Opportunities/Details/{opportunityId}",
            response.Headers.Location?.OriginalString);

        using var verificationScope = factory.Services.CreateScope();
        var verificationDb = verificationScope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        var saved = await verificationDb.JobOpportunities
            .SingleAsync(o => o.Id == opportunityId);

        Assert.Equal(
            "Follow up with recruiter next week.",
            saved.Comments);

        Assert.Equal("Original posting text", saved.OriginalPostingText);
    }

    [Fact]
    public async Task Details_Delete_RemovesOpportunityAndRedirectsToInbox()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

        await factory.InitializeDatabaseAsync();

        int opportunityId;

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var opportunity = new JobSearchAssistant.Web.Models.JobOpportunity
            {
                SourceUrl = "https://example.com/jobs/developer",
                OriginalPostingText = "Original posting text",
                Comments = "Test comment",
                CapturedAtUtc = DateTime.UtcNow
            };

            db.JobOpportunities.Add(opportunity);
            await db.SaveChangesAsync();

            opportunityId = opportunity.Id;
        }

        var pageResponse = await client.GetAsync(
            $"/Opportunities/Details/{opportunityId}");

        pageResponse.EnsureSuccessStatusCode();

        var html = await pageResponse.Content.ReadAsStringAsync();
        var token = ExtractAntiforgeryToken(html);

        var form = new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token
        };

        var response = await client.PostAsync(
            $"/Opportunities/Details/{opportunityId}?handler=Delete",
            new FormUrlEncodedContent(form));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/", response.Headers.Location?.OriginalString);

        using var verificationScope = factory.Services.CreateScope();
        var verificationDb = verificationScope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        Assert.False(await verificationDb.JobOpportunities
            .AnyAsync(o => o.Id == opportunityId));
    }
}
