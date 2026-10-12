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
public class FollowUpsController : Controller
{
    private readonly ApplicationDbContext _db; private readonly IAuditService _audit;
    public FollowUpsController(ApplicationDbContext db, IAuditService audit) { _db = db; _audit = audit; }
    private IQueryable<FollowUp> Scoped() => User.IsInRole("Admin") || User.IsInRole("Manager")
        ? _db.FollowUps.Include(x => x.Customer)
        : _db.FollowUps.Include(x => x.Customer).Where(x => x.AssignedTo == User.FindFirstValue(ClaimTypes.NameIdentifier));
    private async Task LoadCustomers(int? selected = null)
    {
        var q = _db.Customers.AsQueryable();
        if (!User.IsInRole("Admin") && !User.IsInRole("Manager")) q = q.Where(x => x.OwnerId == User.FindFirstValue(ClaimTypes.NameIdentifier));
        ViewBag.CustomerId = new SelectList(await q.ToListAsync(), "CustomerId", "CustomerName", selected);
    }
    public async Task<IActionResult> Index(string? status)
    {
        ViewBag.Status = status;
        var q = Scoped();
        if (!string.IsNullOrWhiteSpace(status)) q = q.Where(x => x.Status == status);
        return View(await q.OrderBy(x => x.FollowUpDate).ToListAsync());
    }
    public async Task<IActionResult> Create() { await LoadCustomers(); return View(new FollowUp()); }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FollowUp model)
    {
        await LoadCustomers(model.CustomerId);
        if (model.FollowUpDate.Date < DateTime.Today) ModelState.AddModelError(nameof(model.FollowUpDate), "Follow-up date cannot be earlier than today.");
        if (!ModelState.IsValid) return View(model);
        model.AssignedTo = User.FindFirstValue(ClaimTypes.NameIdentifier);
        model.CreatedDate = DateTime.UtcNow;
        _db.FollowUps.Add(model); await _db.SaveChangesAsync();
        await _audit.LogAsync("Create", "FollowUp", model.FollowUpId.ToString(), newValue: model.Subject);
        return RedirectToAction(nameof(Index));
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, string status)
    {
        if (!new[] { "Planned", "Completed", "Missed", "Cancelled" }.Contains(status)) return BadRequest();
        var item = await Scoped().FirstOrDefaultAsync(x => x.FollowUpId == id);
        if (item == null) return NotFound();
        var old = item.Status; item.Status = status;
        await _db.SaveChangesAsync();
        await _audit.LogAsync("Update Status", "FollowUp", id.ToString(), old, status);
        return RedirectToAction(nameof(Index));
    }
}
