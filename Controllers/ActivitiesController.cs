using System.Security.Claims;
using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers;

[Authorize]
public class ActivitiesController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IAuditService _audit;
    public ActivitiesController(ApplicationDbContext db, IAuditService audit) { _db = db; _audit = audit; }

    public async Task<IActionResult> Index(string? type)
    {
        ViewBag.Type = type;
        var q = _db.Activities.AsQueryable();
        if (!User.IsInRole("Admin") && !User.IsInRole("Manager"))
            q = q.Where(x => x.AssignedTo == User.FindFirstValue(ClaimTypes.NameIdentifier));
        if (!string.IsNullOrWhiteSpace(type)) q = q.Where(x => x.ActivityType == type);
        return View(await q.OrderByDescending(x => x.ActivityDate).Take(500).ToListAsync());
    }

    public IActionResult Create() => View(new Activity());
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Activity model)
    {
        if (!new[] { "Call", "Meeting", "Email", "Task" }.Contains(model.ActivityType))
            ModelState.AddModelError(nameof(model.ActivityType), "Choose a valid activity type.");
        if (!ModelState.IsValid) return View(model);
        model.AssignedTo = User.FindFirstValue(ClaimTypes.NameIdentifier);
        model.ActivityDate = DateTime.UtcNow;
        _db.Activities.Add(model);
        await _db.SaveChangesAsync();
        await _audit.LogAsync("Create", "Activity", model.ActivityId.ToString(), newValue: model.Subject);
        return RedirectToAction(nameof(Index));
    }
}
