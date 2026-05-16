using System.ComponentModel.DataAnnotations;

namespace IT15_MonticalboFinalProj.Models.ViewModels;

public class AdminUserViewModel
{
    public string? Id { get; set; }

    [Required]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Username { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    public string? Password { get; set; }

    [Required]
    public string Role { get; set; } = "Administrator";

    public string Status { get; set; } = "Active";
    public DateTime CreatedAt { get; set; }
    public DateTime? ArchivedAt { get; set; }

    public int? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
}
