using System.Security.Claims;
using AcxiomCRM.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers;

[Authorize]
public class ReportsController : Controller
{
    private readonly ApplicationDbContext _db;
    public ReportsController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index(DateTime? from, DateTime? to)
    {
        var customers = _db.Customers.AsQueryable();
        var leads = _db.Leads.AsQueryable();
        var opportunities = _db.Opportunities.AsQueryable();
        var followups = _db.FollowUps.AsQueryable();
        var activities = _db.Activities.AsQueryable();
        if (!User.IsInRole("Admin") && !User.IsInRole("Manager"))
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            customers = customers.Where(x => x.OwnerId == userId);
            leads = leads.Where(x => x.AssignedTo == userId);
            opportunities = opportunities.Where(x => x.AssignedTo == userId);
            followups = followups.Where(x => x.AssignedTo == userId);
            activities = activities.Where(x => x.AssignedTo == userId);
        }
        if (from.HasValue) {
            customers = customers.Where(x => x.CreatedDate >= from.Value);
            leads = leads.Where(x => x.CreatedDate >= from.Value);
            opportunities = opportunities.Where(x => x.CreatedDate >= from.Value);
            followups = followups.Where(x => x.FollowUpDate >= from.Value);
            activities = activities.Where(x => x.ActivityDate >= from.Value);
        }
        if (to.HasValue) {
            var exclusiveEnd = to.Value.Date.AddDays(1);
            customers = customers.Where(x => x.CreatedDate < exclusiveEnd);
            leads = leads.Where(x => x.CreatedDate < exclusiveEnd);
            opportunities = opportunities.Where(x => x.CreatedDate < exclusiveEnd);
            followups = followups.Where(x => x.FollowUpDate < exclusiveEnd);
            activities = activities.Where(x => x.ActivityDate < exclusiveEnd);
        }
        ViewBag.From = from?.ToString("yyyy-MM-dd");
        ViewBag.To = to?.ToString("yyyy-MM-dd");
        ViewBag.CustomerCount = await customers.CountAsync();
        ViewBag.LeadCount = await leads.CountAsync();
        ViewBag.ConvertedLeads = await leads.CountAsync(x => x.Status == "Converted");
        ViewBag.FollowUpCount = await followups.CountAsync();
        ViewBag.OverdueFollowUps = await followups.CountAsync(x => x.Status == "Planned" && x.FollowUpDate < DateTime.Today);
        ViewBag.ActivityCount = await activities.CountAsync();
        ViewBag.Opportunities = await opportunities.OrderBy(x => x.Stage).ThenByDescending(x => x.Amount).Take(250).ToListAsync();
        ViewBag.Leads = await leads.OrderByDescending(x => x.CreatedDate).Take(250).ToListAsync();
        ViewBag.FollowUps = await followups.Include(x => x.Customer).OrderBy(x => x.FollowUpDate).Take(250).ToListAsync();
        return View();
    }
}
