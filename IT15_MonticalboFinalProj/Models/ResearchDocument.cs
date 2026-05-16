using System.ComponentModel.DataAnnotations;

namespace IT15_MonticalboFinalProj.Models;

public class ResearchDocument
{
    public int Id { get; set; }
    
    [Required]
    public int ProjectId { get; set; }
    public Project? Project { get; set; }
    
    [Required]
    [StringLength(255)]
    public string Title { get; set; } = string.Empty;
    
    public string? Abstract { get; set; }
    
    public string Version { get; set; } = "1.0";
    
    [Required]
    public string FilePath { get; set; } = string.Empty;
    
    public string? UploadedById { get; set; }
    public ApplicationUser? UploadedBy { get; set; }
    
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    
    public bool IsFinal { get; set; } = false;
}
