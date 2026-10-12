using System.Security.Claims;
using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers;

[Authorize]
public class CustomersController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IAuditService _audit;
    public CustomersController(ApplicationDbContext db, IAuditService audit) { _db = db; _audit = audit; }

    private IQueryable<Customer> Scoped()
    {
        var q = _db.Customers.Include(x => x.Owner).AsQueryable();
        if (!User.IsInRole("Admin") && !User.IsInRole("Manager"))
            q = q.Where(x => x.OwnerId == User.FindFirstValue(ClaimTypes.NameIdentifier));
        return q;
    }

    public async Task<IActionResult> Index(string? search)
    {
        ViewBag.Search = search;
        var q = Scoped();
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(x => x.CustomerName.Contains(search) || x.Email.Contains(search) ||
                             (x.Phone != null && x.Phone.Contains(search)) || (x.CompanyName != null && x.CompanyName.Contains(search)));
        return View(await q.OrderByDescending(x => x.CreatedDate).ToListAsync());
    }

    public IActionResult Create() => View(new Customer());
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Customer model)
    {
        if (await _db.Customers.AnyAsync(x => x.Email == model.Email || x.Phone == model.Phone))
            ModelState.AddModelError("", "A customer with this email or phone already exists.");
        if (!ModelState.IsValid) return View(model);
        model.CustomerCode = "CUS-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        model.OwnerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        model.CreatedBy = model.OwnerId;
        model.CreatedDate = DateTime.UtcNow;
        _db.Customers.Add(model);
        await _db.SaveChangesAsync();
        await _audit.LogAsync("Create", "Customer", model.CustomerId.ToString(), newValue: model.CustomerName);
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(int id)
    {
        var item = await Scoped().FirstOrDefaultAsync(x => x.CustomerId == id);
        return item == null ? NotFound() : View(item);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var item = await Scoped().FirstOrDefaultAsync(x => x.CustomerId == id);
        return item == null ? NotFound() : View(item);
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Customer model)
    {
        if (id != model.CustomerId) return BadRequest();
        var existing = await Scoped().FirstOrDefaultAsync(x => x.CustomerId == id);
        if (existing == null) return NotFound();
        if (await _db.Customers.AnyAsync(x => x.CustomerId != id && (x.Email == model.Email || x.Phone == model.Phone)))
            ModelState.AddModelError("", "Another customer already uses this email or phone.");
        if (!ModelState.IsValid) return View(model);
        var old = existing.CustomerName;
        existing.CustomerName = model.CustomerName; existing.Email = model.Email; existing.Phone = model.Phone;
        existing.CompanyName = model.CompanyName; existing.Address = model.Address; existing.City = model.City;
        existing.State = model.State; existing.Status = model.Status; existing.ModifiedDate = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await _audit.LogAsync("Update", "Customer", id.ToString(), old, existing.CustomerName);
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int id)
    {
        var item = await Scoped().FirstOrDefaultAsync(x => x.CustomerId == id);
        return item == null ? NotFound() : View(item);
    }
    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var item = await Scoped().FirstOrDefaultAsync(x => x.CustomerId == id);
        if (item == null) return NotFound();
        item.Status = "Inactive"; item.ModifiedDate = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await _audit.LogAsync("Deactivate", "Customer", id.ToString(), newValue: item.CustomerName);
        return RedirectToAction(nameof(Index));
    }
}
