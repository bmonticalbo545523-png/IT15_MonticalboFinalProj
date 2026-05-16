using Microsoft.AspNetCore.Identity;

namespace IT15_MonticalboFinalProj.Models;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public string Status { get; set; } = "Active";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Archive workflow
    public bool IsActive { get; set; } = true;
    public DateTime? ArchivedAt { get; set; }

    // Department relationship
    public int? DepartmentId { get; set; }
    public virtual Department? Department { get; set; }

    // Profile fields
    public string? Bio { get; set; }
    public string? Specialization { get; set; }
    public string? ProfilePhotoPath { get; set; }
    public bool IsProfileComplete { get; set; } = false;

    // Student specific
    public string? StudentIdNumber { get; set; }
    public string? ProgramOrCourse { get; set; }
}
