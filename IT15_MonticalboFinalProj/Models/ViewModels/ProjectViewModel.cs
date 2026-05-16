using System.ComponentModel.DataAnnotations;

namespace IT15_MonticalboFinalProj.Models.ViewModels;

public class ProjectViewModel
{
    public int Id { get; set; }

    [Required]
    [Display(Name = "Project Name")]
    public string ProjectName { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Start Date")]
    [DataType(DataType.Date)]
    public DateTime StartDate { get; set; } = DateTime.Today;

    [Display(Name = "End Date")]
    [DataType(DataType.Date)]
    public DateTime? EndDate { get; set; }

    [Required]
    public string Status { get; set; } = "Proposal";

    public string? CreatedByName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ArchivedAt { get; set; }

    public string? AdviserId { get; set; }
    public string? AdviserName { get; set; }
    public bool IsAcceptedByAdviser { get; set; }
    public int MemberCount { get; set; }
}
