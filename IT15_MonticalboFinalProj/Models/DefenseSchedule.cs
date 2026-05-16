namespace IT15_MonticalboFinalProj.Models;

public class DefenseSchedule
{
    public int Id { get; set; }

    // FK → Project (research group)
    public int ProjectId { get; set; }
    public Project? Project { get; set; }

    // Schedule details
    public DateTime ScheduledDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string Location { get; set; } = string.Empty;

    // Status: Pending, Approved by Adviser, Confirmed, Completed, Cancelled
    public string Status { get; set; } = "Pending";

    // Admin who scheduled it
    public string? ScheduledById { get; set; }
    public ApplicationUser? ScheduledBy { get; set; }

    // Adviser who approved
    public string? ApprovedByAdviserId { get; set; }
    public ApplicationUser? ApprovedByAdviser { get; set; }

    public string? Remarks { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
