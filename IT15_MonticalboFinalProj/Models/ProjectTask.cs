namespace IT15_MonticalboFinalProj.Models;

public class ProjectTask
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // Priority: Low, Medium, High
    public string Priority { get; set; } = "Medium";

    // Status: Pending, Submitted, Under Review, Needs Revision, Approved, Overdue
    public string Status { get; set; } = "Pending";

    // Type: Document, Presentation, Other
    public string Type { get; set; } = "Document";

    public DateTime? DueDate { get; set; }

    // FK → Project
    public int ProjectId { get; set; }
    public Project? Project { get; set; }

    // Assigned user (optional if assigned to the whole group)
    public string? AssignedToId { get; set; }
    public ApplicationUser? AssignedTo { get; set; }

    // Creator
    public string? CreatedById { get; set; }
    public ApplicationUser? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // File attachment (sample/template uploaded by adviser)
    public string? AttachmentPath { get; set; }
    public string? AttachmentFileName { get; set; }

    // Sequential ordering for deliverable gating (lower must be approved first)
    public int OrderIndex { get; set; } = 0;

    // Soft-delete / Archive
    public bool IsArchived { get; set; } = false;
    public DateTime? ArchivedAt { get; set; }

    // Navigation
    public ICollection<DeliverableSubmission> Submissions { get; set; } = new List<DeliverableSubmission>();
    public ICollection<DeliverableComment> Comments { get; set; } = new List<DeliverableComment>();
}
