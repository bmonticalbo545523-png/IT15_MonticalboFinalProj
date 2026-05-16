namespace IT15_MonticalboFinalProj.Models;

public class DeliverableComment
{
    public int Id { get; set; }

    // FK -> ProjectTask (Deliverable)
    public int ProjectTaskId { get; set; }
    public ProjectTask? ProjectTask { get; set; }

    // FK -> Author (Adviser or Student)
    public string AuthorId { get; set; } = string.Empty;
    public ApplicationUser? Author { get; set; }

    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
