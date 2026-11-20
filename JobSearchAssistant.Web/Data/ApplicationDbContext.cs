
using JobSearchAssistant.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace JobSearchAssistant.Web.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options)
{
    public DbSet<JobOpportunity> JobOpportunities => Set<JobOpportunity>();
}
