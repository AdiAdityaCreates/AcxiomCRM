using System.Security.Claims;
using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers;

[Authorize]
public class LeadsController : Controller
{
    private readonly ApplicationDbContext _db; private readonly IAuditService _audit;
    private static readonly string[] ValidStatuses = { "New", "Contacted", "Qualified", "Unqualified", "Converted", "Lost" };
    public LeadsController(ApplicationDbContext db, IAuditService audit) { _db = db; _audit = audit; }
    private IQueryable<Lead> Scoped() => User.IsInRole("Admin") || User.IsInRole("Manager")
        ? _db.Leads.Include(x => x.Owner)
        : _db.Leads.Include(x => x.Owner).Where(x => x.AssignedTo == User.FindFirstValue(ClaimTypes.NameIdentifier));

    public async Task<IActionResult> Index(string? search, string? status)
    {
        ViewBag.Search = search; ViewBag.Status = status;
        var q = Scoped();
        if (!string.IsNullOrWhiteSpace(search)) q = q.Where(x => x.LeadName.Contains(search) || (x.CompanyName != null && x.CompanyName.Contains(search)));
        if (!string.IsNullOrWhiteSpace(status)) q = q.Where(x => x.Status == status);
        return View(await q.OrderByDescending(x => x.CreatedDate).ToListAsync());
    }
    public IActionResult Create() => View(new Lead());
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Lead model)
    {
        if (!ValidStatuses.Contains(model.Status)) ModelState.AddModelError(nameof(model.Status), "Choose a valid lead status.");
        if (!ModelState.IsValid) return View(model);
        model.LeadCode = "LED-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        model.AssignedTo = User.FindFirstValue(ClaimTypes.NameIdentifier);
        model.CreatedDate = DateTime.UtcNow;
        _db.Leads.Add(model); await _db.SaveChangesAsync();
        await _audit.LogAsync("Create", "Lead", model.LeadId.ToString(), newValue: model.LeadName);
        return RedirectToAction(nameof(Index));
    }
    public async Task<IActionResult> Edit(int id)
    {
        var item = await Scoped().FirstOrDefaultAsync(x => x.LeadId == id);
        return item == null ? NotFound() : View(item);
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Lead model)
    {
        if (id != model.LeadId) return BadRequest();
        var item = await Scoped().FirstOrDefaultAsync(x => x.LeadId == id);
        if (item == null) return NotFound();
        if (!ValidStatuses.Contains(model.Status)) ModelState.AddModelError(nameof(model.Status), "Choose a valid lead status.");
        if (!ModelState.IsValid) return View(model);
        var old = item.Status;
        item.LeadName = model.LeadName; item.Email = model.Email; item.Phone = model.Phone;
        item.CompanyName = model.CompanyName; item.Source = model.Source; item.Status = model.Status;
        item.Priority = model.Priority; item.ExpectedValue = model.ExpectedValue; item.Notes = model.Notes;
        await _db.SaveChangesAsync();
        await _audit.LogAsync("Update", "Lead", id.ToString(), old, item.Status);
        return RedirectToAction(nameof(Index));
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Convert(int id)
    {
        var lead = await Scoped().FirstOrDefaultAsync(x => x.LeadId == id);
        if (lead == null) return NotFound();
        if (lead.Status != "Qualified") { TempData["Error"] = "Only qualified leads can be converted."; return RedirectToAction(nameof(Index)); }
        if (string.IsNullOrWhiteSpace(lead.Email) || string.IsNullOrWhiteSpace(lead.Phone))
        { TempData["Error"] = "Lead must have email and phone before conversion."; return RedirectToAction(nameof(Index)); }
        var customer = new Customer { CustomerCode = "CUS-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant(),
            CustomerName = lead.LeadName, Email = lead.Email, Phone = lead.Phone, CompanyName = lead.CompanyName,
            OwnerId = lead.AssignedTo, CreatedBy = User.FindFirstValue(ClaimTypes.NameIdentifier) };
        if (await _db.Customers.AnyAsync(x => x.Email == customer.Email || x.Phone == customer.Phone))
        { TempData["Error"] = "A customer already exists with this email or phone."; return RedirectToAction(nameof(Index)); }
        _db.Customers.Add(customer); lead.Status = "Converted";
        await _db.SaveChangesAsync();
        await _audit.LogAsync("Convert", "Lead", id.ToString(), "Qualified", "Converted");
        return RedirectToAction("Index", "Customers");
    }
}
