namespace IT15_MonticalboFinalProj.Models;

public class ProjectMember
{
    public int Id { get; set; }
    
    // FK -> Project
    public int ProjectId { get; set; }
    public Project? Project { get; set; }

    // FK -> Student (ApplicationUser)
    public string StudentId { get; set; } = string.Empty;
    public ApplicationUser? Student { get; set; }

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}
