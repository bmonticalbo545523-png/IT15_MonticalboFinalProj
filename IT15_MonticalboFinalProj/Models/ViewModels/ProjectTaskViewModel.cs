using System.ComponentModel.DataAnnotations;

namespace IT15_MonticalboFinalProj.Models.ViewModels;

public class ProjectTaskViewModel
{
    public int Id { get; set; }

    [Required]
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    [Required]
    public string Priority { get; set; } = "Medium"; // Low, Medium, High

    [Required]
    public string Status { get; set; } = "Pending";

    [Required]
    public string Type { get; set; } = "Document"; // Document, Presentation, Other

    [Display(Name = "Due Date")]
    [DataType(DataType.Date)]
    public DateTime? DueDate { get; set; }

    [Required]
    [Display(Name = "Project")]
    public int ProjectId { get; set; }

    public string? ProjectName { get; set; }

    [Display(Name = "Assigned To")]
    public string? AssignedToId { get; set; }

    public string? AssignedToName { get; set; }

    public DateTime CreatedAt { get; set; }

    // File attachment (sample from adviser)
    public string? AttachmentPath { get; set; }
    public string? AttachmentFileName { get; set; }

    // Sequential ordering
    public int OrderIndex { get; set; }

    // Whether this deliverable is locked (prerequisites not met)
    public bool IsLocked { get; set; } = false;
}
