using System.Security.Claims;
using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers;

[Authorize]
public class OpportunitiesController : Controller
{
    private readonly ApplicationDbContext _db; private readonly IAuditService _audit;
    private static readonly string[] Stages = { "Qualification", "Proposal", "Negotiation", "Won", "Lost" };
    public OpportunitiesController(ApplicationDbContext db, IAuditService audit) { _db = db; _audit = audit; }
    private IQueryable<Opportunity> Scoped() => (User.IsInRole("Admin") || User.IsInRole("Manager"))
        ? _db.Opportunities.Include(x => x.Customer)
        : _db.Opportunities.Include(x => x.Customer).Where(x => x.AssignedTo == User.FindFirstValue(ClaimTypes.NameIdentifier));
    private async Task LoadCustomers(int? selected = null)
    {
        var q = _db.Customers.AsQueryable();
        if (!User.IsInRole("Admin") && !User.IsInRole("Manager")) q = q.Where(x => x.OwnerId == User.FindFirstValue(ClaimTypes.NameIdentifier));
        ViewBag.CustomerId = new SelectList(await q.OrderBy(x => x.CustomerName).ToListAsync(), "CustomerId", "CustomerName", selected);
    }
    public async Task<IActionResult> Index(string? search, string? stage)
    {
        ViewBag.Search = search; ViewBag.Stage = stage;
        var q = Scoped();
        if (!string.IsNullOrWhiteSpace(search)) q = q.Where(x => x.OpportunityName.Contains(search) || (x.Customer != null && x.Customer.CustomerName.Contains(search)));
        if (!string.IsNullOrWhiteSpace(stage)) q = q.Where(x => x.Stage == stage);
        return View(await q.OrderBy(x => x.ExpectedCloseDate).ToListAsync());
    }
    public async Task<IActionResult> Create() { await LoadCustomers(); return View(new Opportunity()); }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Opportunity model)
    {
        await LoadCustomers(model.CustomerId);
        if (!Stages.Contains(model.Stage)) ModelState.AddModelError(nameof(model.Stage), "Invalid opportunity stage.");
        if (model.Amount <= 0) ModelState.AddModelError(nameof(model.Amount), "Opportunity Amount must be greater than 0.");
        if (model.ExpectedCloseDate.Date < DateTime.Today && model.Stage != "Won" && model.Stage != "Lost")
            ModelState.AddModelError(nameof(model.ExpectedCloseDate), "Expected Close Date cannot be in the past.");
        if (!ModelState.IsValid) return View(model);
        model.AssignedTo = User.FindFirstValue(ClaimTypes.NameIdentifier);
        model.Status = model.Stage is "Won" or "Lost" ? model.Stage : "Open";
        _db.Opportunities.Add(model); await _db.SaveChangesAsync();
        await _audit.LogAsync("Create", "Opportunity", model.OpportunityId.ToString(), newValue: model.OpportunityName);
        return RedirectToAction(nameof(Index));
    }
    public async Task<IActionResult> Edit(int id)
    {
        var item = await Scoped().FirstOrDefaultAsync(x => x.OpportunityId == id);
        if (item == null) return NotFound();
        await LoadCustomers(item.CustomerId); return View(item);
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Opportunity model)
    {
        if (id != model.OpportunityId) return BadRequest();
        var item = await Scoped().FirstOrDefaultAsync(x => x.OpportunityId == id);
        if (item == null) return NotFound();
        await LoadCustomers(model.CustomerId);
        if (!Stages.Contains(model.Stage)) ModelState.AddModelError(nameof(model.Stage), "Invalid opportunity stage.");
        if (model.Amount <= 0) ModelState.AddModelError(nameof(model.Amount), "Opportunity Amount must be greater than 0.");
        if (model.ExpectedCloseDate.Date < DateTime.Today && model.Stage != "Won" && model.Stage != "Lost")
            ModelState.AddModelError(nameof(model.ExpectedCloseDate), "Expected Close Date cannot be in the past.");
        if (!ModelState.IsValid) return View(model);
        var old = item.Stage;
        item.OpportunityName = model.OpportunityName; item.CustomerId = model.CustomerId; item.Amount = model.Amount;
        item.Stage = model.Stage; item.Probability = model.Probability; item.ExpectedCloseDate = model.ExpectedCloseDate;
        item.Status = model.Stage is "Won" or "Lost" ? model.Stage : "Open"; item.Notes = model.Notes;
        await _db.SaveChangesAsync();
        await _audit.LogAsync("Update", "Opportunity", id.ToString(), old, item.Stage);
        return RedirectToAction(nameof(Index));
    }
}
