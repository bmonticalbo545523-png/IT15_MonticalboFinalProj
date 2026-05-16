namespace IT15_MonticalboFinalProj.Models;

public class Announcement
{
    public int Id { get; set; }

    // FK -> Project (Research Group)
    public int ProjectId { get; set; }
    public Project? Project { get; set; }

    // FK -> Author (Usually Adviser)
    public string AuthorId { get; set; } = string.Empty;
    public ApplicationUser? Author { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
