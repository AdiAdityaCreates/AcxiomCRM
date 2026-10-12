using System.Security.Claims;
using AcxiomCRM.Data;
using AcxiomCRM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers.Api;

[ApiController, Route("api/opportunities"), Authorize]
public class OpportunitiesApiController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public OpportunitiesApiController(ApplicationDbContext db) => _db = db;
    public record OpportunityDto(int OpportunityId, string OpportunityName, decimal Amount, string Stage, int Probability, DateTime ExpectedCloseDate);
    public record CreateOpportunityDto(string OpportunityName, int? CustomerId, decimal Amount, string Stage, int Probability, DateTime ExpectedCloseDate);
    private IQueryable<Opportunity> Scoped() => User.IsInRole("Admin") || User.IsInRole("Manager") ? _db.Opportunities :
        _db.Opportunities.Where(x => x.AssignedTo == User.FindFirstValue(ClaimTypes.NameIdentifier));

    [HttpGet]
    public async Task<ActionResult<IEnumerable<OpportunityDto>>> Get() => Ok(await Scoped().OrderBy(x => x.ExpectedCloseDate)
        .Select(x => new OpportunityDto(x.OpportunityId, x.OpportunityName, x.Amount, x.Stage, x.Probability, x.ExpectedCloseDate)).ToListAsync());

    [HttpPost]
    public async Task<ActionResult<OpportunityDto>> Create(CreateOpportunityDto dto)
    {
        var stages = new[] { "Qualification", "Proposal", "Negotiation", "Won", "Lost" };
        if (string.IsNullOrWhiteSpace(dto.OpportunityName) || dto.Amount <= 0 || dto.Probability < 0 || dto.Probability > 100 || !stages.Contains(dto.Stage))
            return BadRequest(new { message = "Invalid opportunity fields. Amount must be positive and probability must be 0–100." });
        if (dto.ExpectedCloseDate.Date < DateTime.Today && dto.Stage is not ("Won" or "Lost"))
            return BadRequest(new { message = "Expected close date cannot be in the past for an active opportunity." });
        var item = new Opportunity { OpportunityName = dto.OpportunityName.Trim(), CustomerId = dto.CustomerId,
            Amount = dto.Amount, Stage = dto.Stage, Probability = dto.Probability, ExpectedCloseDate = dto.ExpectedCloseDate,
            Status = dto.Stage is "Won" or "Lost" ? dto.Stage : "Open", AssignedTo = User.FindFirstValue(ClaimTypes.NameIdentifier) };
        _db.Opportunities.Add(item); await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = item.OpportunityId },
            new OpportunityDto(item.OpportunityId, item.OpportunityName, item.Amount, item.Stage, item.Probability, item.ExpectedCloseDate));
    }
}
