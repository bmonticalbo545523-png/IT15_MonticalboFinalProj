using System.ComponentModel.DataAnnotations;

namespace IT15_MonticalboFinalProj.Models;

public class Department
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public virtual ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
}
