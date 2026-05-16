using System.ComponentModel.DataAnnotations;

namespace IT15_MonticalboFinalProj.Models;

public class ProjectMessage
{
    public int Id { get; set; }
    
    [Required]
    public int ProjectId { get; set; }
    public Project? Project { get; set; }
    
    [Required]
    public string SenderId { get; set; } = string.Empty;
    public ApplicationUser? Sender { get; set; }
    
    [Required]
    public string Content { get; set; } = string.Empty;
    
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
