using System.Security.Claims;
using AcxiomCRM.Data;
using AcxiomCRM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers.Api;

[ApiController, Route("api/leads"), Authorize]
public class LeadsApiController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public LeadsApiController(ApplicationDbContext db) => _db = db;
    public record LeadDto(int LeadId, string LeadCode, string LeadName, string? Email, string? Phone, string Status, decimal ExpectedValue);
    public record CreateLeadDto(string LeadName, string? Email, string? Phone, string? CompanyName, string Source, string Status, decimal ExpectedValue);
    private IQueryable<Lead> Scoped() => User.IsInRole("Admin") || User.IsInRole("Manager") ? _db.Leads :
        _db.Leads.Where(x => x.AssignedTo == User.FindFirstValue(ClaimTypes.NameIdentifier));

    [HttpGet]
    public async Task<ActionResult<IEnumerable<LeadDto>>> Get() => Ok(await Scoped().OrderByDescending(x => x.CreatedDate)
        .Select(x => new LeadDto(x.LeadId, x.LeadCode, x.LeadName, x.Email, x.Phone, x.Status, x.ExpectedValue)).ToListAsync());

    [HttpPost]
    public async Task<ActionResult<LeadDto>> Create(CreateLeadDto dto)
    {
        var valid = new[] { "New", "Contacted", "Qualified", "Unqualified", "Converted", "Lost" };
        if (string.IsNullOrWhiteSpace(dto.LeadName) || !valid.Contains(dto.Status))
            return BadRequest(new { message = "Lead name and valid status are required." });
        if (dto.ExpectedValue < 0) return BadRequest(new { message = "Expected value cannot be negative." });
        var item = new Lead { LeadCode = "LED-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant(),
            LeadName = dto.LeadName.Trim(), Email = dto.Email, Phone = dto.Phone, CompanyName = dto.CompanyName,
            Source = dto.Source, Status = dto.Status, ExpectedValue = dto.ExpectedValue,
            AssignedTo = User.FindFirstValue(ClaimTypes.NameIdentifier) };
        _db.Leads.Add(item); await _db.SaveChangesAsync();
        var result = new LeadDto(item.LeadId, item.LeadCode, item.LeadName, item.Email, item.Phone, item.Status, item.ExpectedValue);
        return CreatedAtAction(nameof(Get), new { id = item.LeadId }, result);
    }
}
