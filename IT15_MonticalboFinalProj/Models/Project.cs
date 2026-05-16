namespace IT15_MonticalboFinalProj.Models;

public class Project
{
    public int Id { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string Status { get; set; } = "Proposal"; // Proposal, In Progress, For Defense, Completed
    public int ProgressPercentage { get; set; } = 0;
    public string CurrentStage { get; set; } = "Requirement Gathering"; 
    // Stages: Requirement Gathering, Design, Development, Testing, Implementation
    public string? CreatedById { get; set; }
    public ApplicationUser? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Adviser Assignment
    public string? AdviserId { get; set; }
    public ApplicationUser? Adviser { get; set; }
    public bool IsAcceptedByAdviser { get; set; } = false;

    // Soft-delete / Archive
    public bool IsArchived { get; set; } = false;
    public DateTime? ArchivedAt { get; set; }

    // Department Isolation
    public int? DepartmentId { get; set; }
    public Department? Department { get; set; }

    // Navigation
    public ICollection<ProjectTask> Tasks { get; set; } = new List<ProjectTask>();
    public ICollection<ProjectMember> Members { get; set; } = new List<ProjectMember>();
    public ICollection<Announcement> Announcements { get; set; } = new List<Announcement>();
    public ICollection<ProjectMilestone> Milestones { get; set; } = new List<ProjectMilestone>();
    public ICollection<ProjectStageLog> StageLogs { get; set; } = new List<ProjectStageLog>();
    public ICollection<ResearchDocument> Documents { get; set; } = new List<ResearchDocument>();
    public ICollection<ProjectMessage> Messages { get; set; } = new List<ProjectMessage>();
    public ICollection<DefenseSchedule> DefenseSchedules { get; set; } = new List<DefenseSchedule>();
}
