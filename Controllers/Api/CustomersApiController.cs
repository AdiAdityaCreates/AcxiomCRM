using System.Security.Claims;
using AcxiomCRM.Data;
using AcxiomCRM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers.Api;

[ApiController]
[Route("api/customers")]
[Authorize]
public class CustomersApiController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public CustomersApiController(ApplicationDbContext db) => _db = db;

    public record CustomerDto(int CustomerId, string CustomerCode, string CustomerName, string Email,
        string Phone, string? CompanyName, string Status, DateTime CreatedDate);
    public record CreateCustomerDto(string CustomerName, string Email, string Phone, string? CompanyName,
        string? Address, string? City, string? State);

    private IQueryable<Customer> Scoped() => User.IsInRole("Admin") || User.IsInRole("Manager")
        ? _db.Customers
        : _db.Customers.Where(x => x.OwnerId == User.FindFirstValue(ClaimTypes.NameIdentifier));

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CustomerDto>>> Get([FromQuery] string? search)
    {
        var q = Scoped();
        if (!string.IsNullOrWhiteSpace(search)) q = q.Where(x => x.CustomerName.Contains(search) || x.Email.Contains(search));
        return Ok(await q.OrderBy(x => x.CustomerName).Select(x => new CustomerDto(
            x.CustomerId, x.CustomerCode, x.CustomerName, x.Email, x.Phone, x.CompanyName, x.Status, x.CreatedDate)).ToListAsync());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CustomerDto>> GetById(int id)
    {
        var x = await Scoped().FirstOrDefaultAsync(c => c.CustomerId == id);
        return x == null ? NotFound() : Ok(new CustomerDto(x.CustomerId, x.CustomerCode, x.CustomerName, x.Email, x.Phone, x.CompanyName, x.Status, x.CreatedDate));
    }

    [HttpPost]
    public async Task<ActionResult<CustomerDto>> Create(CreateCustomerDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.CustomerName) || !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(dto.Email))
            return BadRequest(new { message = "A customer name and valid email are required." });
        if (string.IsNullOrWhiteSpace(dto.Phone) || !System.Text.RegularExpressions.Regex.IsMatch(dto.Phone, @"^[0-9+\-\s()]{10,20}$"))
            return BadRequest(new { message = "Enter a valid phone number." });
        if (await _db.Customers.AnyAsync(x => x.Email == dto.Email || x.Phone == dto.Phone))
            return Conflict(new { message = "Customer email or phone already exists." });
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var item = new Customer { CustomerCode = "CUS-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant(),
            CustomerName = dto.CustomerName.Trim(), Email = dto.Email.Trim(), Phone = dto.Phone.Trim(),
            CompanyName = dto.CompanyName, Address = dto.Address, City = dto.City, State = dto.State,
            OwnerId = userId, CreatedBy = userId };
        _db.Customers.Add(item); await _db.SaveChangesAsync();
        var result = new CustomerDto(item.CustomerId, item.CustomerCode, item.CustomerName, item.Email, item.Phone, item.CompanyName, item.Status, item.CreatedDate);
        return CreatedAtAction(nameof(GetById), new { id = item.CustomerId }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, CreateCustomerDto dto)
    {
        var item = await Scoped().FirstOrDefaultAsync(x => x.CustomerId == id);
        if (item == null) return NotFound();
        if (string.IsNullOrWhiteSpace(dto.CustomerName) || !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(dto.Email))
            return BadRequest(new { message = "A customer name and valid email are required." });
        if (await _db.Customers.AnyAsync(x => x.CustomerId != id && (x.Email == dto.Email || x.Phone == dto.Phone)))
            return Conflict(new { message = "Customer email or phone already exists." });
        item.CustomerName = dto.CustomerName.Trim(); item.Email = dto.Email.Trim(); item.Phone = dto.Phone.Trim();
        item.CompanyName = dto.CompanyName; item.Address = dto.Address; item.City = dto.City; item.State = dto.State;
        item.ModifiedDate = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await Scoped().FirstOrDefaultAsync(x => x.CustomerId == id);
        if (item == null) return NotFound();
        item.Status = "Inactive"; item.ModifiedDate = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
