using IT15_MonticalboFinalProj.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace IT15_MonticalboFinalProj.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Project> Projects { get; set; }
    public DbSet<ProjectTask> ProjectTasks { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<SystemSetting> SystemSettings { get; set; }
    public DbSet<ProjectMember> ProjectMembers { get; set; }
    public DbSet<DeliverableSubmission> DeliverableSubmissions { get; set; }
    public DbSet<DeliverableComment> DeliverableComments { get; set; }
    public DbSet<Announcement> Announcements { get; set; }
    public DbSet<Department> Departments { get; set; }
    public DbSet<ProjectMilestone> ProjectMilestones { get; set; }
    public DbSet<ProjectStageLog> ProjectStageLogs { get; set; }
    public DbSet<ResearchDocument> ResearchDocuments { get; set; }
    public DbSet<ProjectMessage> ProjectMessages { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<DefenseSchedule> DefenseSchedules { get; set; }
    public DbSet<AdviserAvailability> AdviserAvailabilities { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>()
            .HasOne(u => u.Department)
            .WithMany(d => d.Users)
            .HasForeignKey(u => u.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Project>()
            .HasOne(p => p.CreatedBy)
            .WithMany()
            .HasForeignKey(p => p.CreatedById)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<AuditLog>()
            .HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<ProjectTask>()
            .HasOne(t => t.Project)
            .WithMany(p => p.Tasks)
            .HasForeignKey(t => t.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ProjectTask>()
            .HasOne(t => t.AssignedTo)
            .WithMany()
            .HasForeignKey(t => t.AssignedToId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ProjectTask>()
            .HasOne(t => t.CreatedBy)
            .WithMany()
            .HasForeignKey(t => t.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);

        // Adviser Relationship
        builder.Entity<Project>()
            .HasOne(p => p.Adviser)
            .WithMany()
            .HasForeignKey(p => p.AdviserId)
            .OnDelete(DeleteBehavior.NoAction);

        // ProjectMember Relationships
        builder.Entity<ProjectMember>()
            .HasOne(pm => pm.Project)
            .WithMany(p => p.Members)
            .HasForeignKey(pm => pm.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ProjectMember>()
            .HasOne(pm => pm.Student)
            .WithMany()
            .HasForeignKey(pm => pm.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        // DeliverableSubmission Relationships
        builder.Entity<DeliverableSubmission>()
            .HasOne(ds => ds.ProjectTask)
            .WithMany(pt => pt.Submissions)
            .HasForeignKey(ds => ds.ProjectTaskId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<DeliverableSubmission>()
            .HasOne(ds => ds.Student)
            .WithMany()
            .HasForeignKey(ds => ds.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        // DeliverableComment Relationships
        builder.Entity<DeliverableComment>()
            .HasOne(dc => dc.ProjectTask)
            .WithMany(pt => pt.Comments)
            .HasForeignKey(dc => dc.ProjectTaskId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<DeliverableComment>()
            .HasOne(dc => dc.Author)
            .WithMany()
            .HasForeignKey(dc => dc.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        // Announcement Relationships
        builder.Entity<Announcement>()
            .HasOne(a => a.Project)
            .WithMany(p => p.Announcements)
            .HasForeignKey(a => a.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Announcement>()
            .HasOne(a => a.Author)
            .WithMany()
            .HasForeignKey(a => a.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        // Project Department Isolation
        builder.Entity<Project>()
            .HasOne(p => p.Department)
            .WithMany()
            .HasForeignKey(p => p.DepartmentId)
            .OnDelete(DeleteBehavior.NoAction);

        // New Module Relationships
        builder.Entity<ProjectMilestone>()
            .HasOne(m => m.Project)
            .WithMany(p => p.Milestones)
            .HasForeignKey(m => m.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ProjectStageLog>()
            .HasOne(l => l.Project)
            .WithMany(p => p.StageLogs)
            .HasForeignKey(l => l.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ResearchDocument>()
            .HasOne(d => d.Project)
            .WithMany(p => p.Documents)
            .HasForeignKey(d => d.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ResearchDocument>()
            .HasOne(d => d.UploadedBy)
            .WithMany()
            .HasForeignKey(d => d.UploadedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ProjectMessage>()
            .HasOne(m => m.Project)
            .WithMany(p => p.Messages)
            .HasForeignKey(m => m.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ProjectMessage>()
            .HasOne(m => m.Sender)
            .WithMany()
            .HasForeignKey(m => m.SenderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Notification>()
            .HasOne(n => n.User)
            .WithMany()
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Defense Schedule Relationships
        builder.Entity<DefenseSchedule>()
            .HasOne(ds => ds.Project)
            .WithMany(p => p.DefenseSchedules)
            .HasForeignKey(ds => ds.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<DefenseSchedule>()
            .HasOne(ds => ds.ScheduledBy)
            .WithMany()
            .HasForeignKey(ds => ds.ScheduledById)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<DefenseSchedule>()
            .HasOne(ds => ds.ApprovedByAdviser)
            .WithMany()
            .HasForeignKey(ds => ds.ApprovedByAdviserId)
            .OnDelete(DeleteBehavior.NoAction);

        // Adviser Availability Relationships
        builder.Entity<AdviserAvailability>()
            .HasOne(aa => aa.Adviser)
            .WithMany()
            .HasForeignKey(aa => aa.AdviserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
