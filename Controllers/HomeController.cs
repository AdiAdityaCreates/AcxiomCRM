using System.Diagnostics;
using AcxiomCRM.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _db;
    public HomeController(ApplicationDbContext db) => _db = db;

    [Authorize]
    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var isAdminOrManager = User.IsInRole("Admin") || User.IsInRole("Manager");
        var customers = _db.Customers.AsQueryable();
        var leads = _db.Leads.AsQueryable();
        var opportunities = _db.Opportunities.AsQueryable();
        var followups = _db.FollowUps.AsQueryable();
        if (!isAdminOrManager)
        {
            customers = customers.Where(x => x.OwnerId == userId);
            leads = leads.Where(x => x.AssignedTo == userId);
            opportunities = opportunities.Where(x => x.AssignedTo == userId);
            followups = followups.Where(x => x.AssignedTo == userId);
        }
        ViewBag.CustomerCount = await customers.CountAsync();
        ViewBag.LeadCount = await leads.CountAsync();
        ViewBag.OpenLeads = await leads.CountAsync(x => x.Status != "Lost" && x.Status != "Converted" && x.Status != "Unqualified");
        ViewBag.OpportunityCount = await opportunities.CountAsync();
        ViewBag.OpenOpportunities = await opportunities.CountAsync(x => x.Status == "Open");
        ViewBag.Won = await opportunities.CountAsync(x => x.Stage == "Won");
        ViewBag.Lost = await opportunities.CountAsync(x => x.Stage == "Lost");
        ViewBag.Pipeline = await opportunities.Where(x => x.Status == "Open").SumAsync(x => (decimal?)x.Amount) ?? 0m;
        ViewBag.LeadLabels = new[] { "New", "Contacted", "Qualified", "Lost", "Converted" };
        var leadValues = new List<int>();
        foreach (var label in (string[])ViewBag.LeadLabels)
            leadValues.Add(await leads.CountAsync(x => x.Status == label));
        ViewBag.LeadValues = leadValues.ToArray();
        ViewBag.StageLabels = new[] { "Qualification", "Proposal", "Negotiation", "Won", "Lost" };
        var stageValues = new List<int>();
        foreach (var label in (string[])ViewBag.StageLabels)
            stageValues.Add(await opportunities.CountAsync(x => x.Stage == label));
        ViewBag.StageValues = stageValues.ToArray();
        ViewBag.Upcoming = await followups.Where(x => x.Status == "Planned").OrderBy(x => x.FollowUpDate).Take(5).ToListAsync();
        return View();
    }

    [AllowAnonymous]
    public IActionResult Error() => View();
}
