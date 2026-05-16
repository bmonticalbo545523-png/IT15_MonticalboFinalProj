namespace IT15_MonticalboFinalProj.Models;

public class DeliverableSubmission
{
    public int Id { get; set; }

    // FK -> ProjectTask (Deliverable)
    public int ProjectTaskId { get; set; }
    public ProjectTask? ProjectTask { get; set; }

    // FK -> Student who submitted
    public string StudentId { get; set; } = string.Empty;
    public ApplicationUser? Student { get; set; }

    public string? SubmissionNote { get; set; }
    
    // Physical path or relative path to the uploaded document
    public string? FilePath { get; set; }
    public string? OriginalFileName { get; set; }

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
}
