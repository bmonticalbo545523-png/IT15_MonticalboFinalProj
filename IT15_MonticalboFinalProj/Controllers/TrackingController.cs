using IT15_MonticalboFinalProj.Models;
using IT15_MonticalboFinalProj.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IT15_MonticalboFinalProj.Controllers;

[Authorize]
public class TrackingController : Controller
{
    private readonly IProjectRepository _projectRepo;
    private readonly IProjectMilestoneRepository _milestoneRepo;
    private readonly IProjectStageRepository _stageRepo;
    private readonly IAuditLogRepository _auditRepo;
    private readonly UserManager<ApplicationUser> _userManager;

    public TrackingController(
        IProjectRepository projectRepo,
        IProjectMilestoneRepository milestoneRepo,
        IProjectStageRepository stageRepo,
        IAuditLogRepository auditRepo,
        UserManager<ApplicationUser> userManager)
    {
        _projectRepo = projectRepo;
        _milestoneRepo = milestoneRepo;
        _stageRepo = stageRepo;
        _auditRepo = auditRepo;
        _userManager = userManager;
    }

    private async Task<bool> IsAuthorizedForProject(int projectId, ApplicationUser user)
    {
        if (await _userManager.IsInRoleAsync(user, "SuperAdmin") || await _userManager.IsInRoleAsync(user, "Administrator")) return true;
        var project = await _projectRepo.GetByIdAsync(projectId);
        if (project != null && project.AdviserId == user.Id) return true;
        var studentProjects = await _projectRepo.GetByStudentAsync(user.Id);
        return studentProjects.Any(p => p.Id == projectId);
    }

    public async Task<IActionResult> ProjectDetails(int id)
    {
        Project? project = null;
        var me = await _userManager.GetUserAsync(User);
        
        if (id == 0)
        {
            var studentProjects = await _projectRepo.GetByStudentAsync(me?.Id ?? "");
            project = studentProjects.FirstOrDefault();
            if (project == null) return View("NoProject");
            id = project.Id;
        }
        else
        {
            project = await _projectRepo.GetByIdAsync(id);
        }

        if (project == null) return NotFound();
        if (me == null || !await IsAuthorizedForProject(project.Id, me)) return Unauthorized();

        ViewBag.Milestones = await _milestoneRepo.GetByProjectAsync(id);
        ViewBag.StageLogs = await _stageRepo.GetByProjectAsync(id);
        ViewBag.PreparedBy = me?.FullName ?? "User";
        
        return View(project);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateProgress(int id, int progress)
    {
        var project = await _projectRepo.GetByIdAsync(id);
        if (project == null) return Json(new { success = false, message = "Project not found" });

        var me = await _userManager.GetUserAsync(User);
        if (me == null || !await IsAuthorizedForProject(id, me)) return Json(new { success = false, message = "Unauthorized" });

        project.ProgressPercentage = progress;
        await _projectRepo.UpdateAsync(project);
        
        await _auditRepo.AddAsync(new AuditLog { UserId = me?.Id, Action = $"Updated progress for {project.ProjectName} to {progress}%", Timestamp = DateTime.UtcNow });

        return Json(new { success = true, message = "Progress updated" });
    }

    [HttpPost]
    public async Task<IActionResult> AddMilestone(ProjectMilestone milestone)
    {
        if (!ModelState.IsValid) return Json(new { success = false, message = "Invalid data" });

        var me = await _userManager.GetUserAsync(User);
        if (me == null || !await IsAuthorizedForProject(milestone.ProjectId, me)) return Json(new { success = false, message = "Unauthorized" });

        await _milestoneRepo.AddAsync(milestone);
        return Json(new { success = true, message = "Milestone added" });
    }

    [HttpPost]
    public async Task<IActionResult> ToggleMilestone(int id)
    {
        var m = await _milestoneRepo.GetByIdAsync(id);
        if (m == null) return Json(new { success = false, message = "Not found" });

        var me = await _userManager.GetUserAsync(User);
        if (me == null || !await IsAuthorizedForProject(m.ProjectId, me)) return Json(new { success = false, message = "Unauthorized" });

        m.IsCompleted = !m.IsCompleted;
        m.CompletedAt = m.IsCompleted ? DateTime.UtcNow : null;
        await _milestoneRepo.UpdateAsync(m);

        return Json(new { success = true, isCompleted = m.IsCompleted });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateStage(int projectId, string stageName, string notes)
    {
        var project = await _projectRepo.GetByIdAsync(projectId);
        if (project == null) return Json(new { success = false, message = "Project not found" });

        var me = await _userManager.GetUserAsync(User);
        if (me == null || !await IsAuthorizedForProject(projectId, me)) return Json(new { success = false, message = "Unauthorized" });

        var currentLog = await _stageRepo.GetCurrentStageAsync(projectId);
        if (currentLog != null)
        {
            currentLog.Status = "Completed";
            currentLog.FinishedAt = DateTime.UtcNow;
            // No direct update for stage logs needed if tracking is purely additive, 
            // but we'll assume we update the old one first.
        }

        project.CurrentStage = stageName;
        
        string[] stages = { "Requirement Gathering", "Design", "Development", "Testing", "Implementation" };
        int stageIndex = Array.IndexOf(stages, stageName);
        if (stageIndex >= 0)
        {
            project.ProgressPercentage = (stageIndex + 1) * 20;
        }

        await _projectRepo.UpdateAsync(project);

        var newLog = new ProjectStageLog
        {
            ProjectId = projectId,
            StageName = stageName,
            Status = "Current",
            StartedAt = DateTime.UtcNow,
            Notes = notes
        };
        await _stageRepo.AddLogAsync(newLog);

        return Json(new { success = true, message = $"Moved to {stageName}" });
    }
}
