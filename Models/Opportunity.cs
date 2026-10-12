using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AcxiomCRM.Models;

public class Opportunity
{
    public int OpportunityId { get; set; }
    [Required, StringLength(150)]
    public string OpportunityName { get; set; } = "";
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public int? LeadId { get; set; }
    [Range(0.01, 1000000000000)]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }
    [Required]
    public string Stage { get; set; } = "Qualification";
    [Range(0, 100)]
    public int Probability { get; set; } = 10;
    [DataType(DataType.Date)]
    public DateTime ExpectedCloseDate { get; set; } = DateTime.Today.AddDays(30);
    public string Status { get; set; } = "Open";
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public string? AssignedTo { get; set; }
    public ApplicationUser? Owner { get; set; }
    [StringLength(1000)]
    public string? Notes { get; set; }
    [NotMapped]
    public decimal WeightedPipeline => Amount * Probability / 100m;
}
