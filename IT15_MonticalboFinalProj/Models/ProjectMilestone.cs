using System.ComponentModel.DataAnnotations;

namespace IT15_MonticalboFinalProj.Models;

public class ProjectMilestone
{
    public int Id { get; set; }
    
    [Required]
    public int ProjectId { get; set; }
    public Project? Project { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Title { get; set; } = string.Empty;
    
    public string Description { get; set; } = string.Empty;
    
    [Required]
    public DateTime TargetDate { get; set; }
    
    public bool IsCompleted { get; set; } = false;
    public DateTime? CompletedAt { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
