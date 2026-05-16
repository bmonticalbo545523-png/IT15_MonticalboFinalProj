using System.ComponentModel.DataAnnotations;

namespace IT15_MonticalboFinalProj.Models;

public class ProjectStageLog
{
    public int Id { get; set; }
    
    [Required]
    public int ProjectId { get; set; }
    public Project? Project { get; set; }
    
    [Required]
    public string StageName { get; set; } = string.Empty; // Requirement Gathering, Design, etc.
    
    public string Status { get; set; } = "Current"; // Current, Completed
    
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? FinishedAt { get; set; }
    
    public string? Notes { get; set; }
}
