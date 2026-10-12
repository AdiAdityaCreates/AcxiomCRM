using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.Models;

public class FollowUp
{
    public int FollowUpId { get; set; }
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public int? LeadId { get; set; }
    [Required, DataType(DataType.Date)]
    public DateTime FollowUpDate { get; set; } = DateTime.Today.AddDays(1);
    [Required, StringLength(150)]
    public string Subject { get; set; } = "";
    [Required]
    public string FollowUpType { get; set; } = "Call";
    [Required]
    public string Status { get; set; } = "Planned";
    [StringLength(1000)]
    public string? Remarks { get; set; }
    public string? AssignedTo { get; set; }
    public ApplicationUser? Owner { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}
