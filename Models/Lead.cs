using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.Models;

public class Lead
{
    public int LeadId { get; set; }
    [Required, StringLength(20)]
    public string LeadCode { get; set; } = "";
    [Required, StringLength(120)]
    public string LeadName { get; set; } = "";
    [EmailAddress, StringLength(254)]
    public string? Email { get; set; }
    [RegularExpression(@"^[0-9+\-\s()]{10,20}$", ErrorMessage = "Enter a valid phone number.")]
    public string? Phone { get; set; }
    [StringLength(100)]
    public string? CompanyName { get; set; }
    [StringLength(80)]
    public string Source { get; set; } = "Website";
    [Required]
    public string Status { get; set; } = "New";
    [Required]
    public string Priority { get; set; } = "Normal";
    [Range(0, 1000000000)]
    public decimal ExpectedValue { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public string? AssignedTo { get; set; }
    public ApplicationUser? Owner { get; set; }
    [StringLength(1000)]
    public string? Notes { get; set; }
}
