using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.Models;

public class Customer
{
    public int CustomerId { get; set; }
    [Required, StringLength(20)]
    public string CustomerCode { get; set; } = "";
    [Required, StringLength(120)]
    public string CustomerName { get; set; } = "";
    [Required, EmailAddress, StringLength(254)]
    public string Email { get; set; } = "";
    [Required, RegularExpression(@"^[0-9+\-\s()]{10,20}$", ErrorMessage = "Enter a valid phone number.")]
    public string Phone { get; set; } = "";
    [StringLength(160)]
    public string? CompanyName { get; set; }
    [StringLength(250)]
    public string? Address { get; set; }
    [StringLength(80)]
    public string? City { get; set; }
    [StringLength(80)]
    public string? State { get; set; }
    [Required]
    public string Status { get; set; } = "Active";
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public string? OwnerId { get; set; }
    public ApplicationUser? Owner { get; set; }
    public ICollection<Opportunity> Opportunities { get; set; } = new List<Opportunity>();
}
