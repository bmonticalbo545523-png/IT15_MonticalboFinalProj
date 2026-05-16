using IT15_MonticalboFinalProj.Models;

namespace IT15_MonticalboFinalProj.Models.ViewModels;

public class DashboardStatsViewModel
{
    public int TotalAdmins { get; set; }
    public int TotalResearchAdvisers { get; set; }
    public int TotalStudents { get; set; }
    public int TotalUsers { get; set; }
    public int TotalProjects { get; set; }
    public int TotalTasks { get; set; }
    public int TotalAuditLogs { get; set; }
    public int ActiveProjects { get; set; }
    public int PendingProjects { get; set; }
    public int CompletedProjects { get; set; }
    public int TasksTodo { get; set; }
    public int TasksInProgress { get; set; }
    public int TasksDone { get; set; }
    public List<AuditLog> RecentLogs { get; set; } = new();
    public string SystemName { get; set; } = "ResearchHub";
}
