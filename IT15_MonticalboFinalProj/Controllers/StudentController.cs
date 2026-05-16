using IT15_MonticalboFinalProj.Models;
using IT15_MonticalboFinalProj.Models.ViewModels;
using IT15_MonticalboFinalProj.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IT15_MonticalboFinalProj.Controllers;

[Authorize(Roles = "Student")]
public class StudentController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IProjectRepository _projectRepo;
    private readonly IProjectTaskRepository _taskRepo;
    private readonly IAuditLogRepository _auditRepo;
    private readonly IAnnouncementRepository _announcementRepo;
    private readonly IDeliverableSubmissionRepository _submissionRepo;
    private readonly IDeliverableCommentRepository _commentRepo;
    private readonly IDefenseScheduleRepository _defenseRepo;
    private readonly IWebHostEnvironment _environment;

    public StudentController(
        UserManager<ApplicationUser> userManager,
        IProjectRepository projectRepo,
        IProjectTaskRepository taskRepo,
        IAuditLogRepository auditRepo,
        IAnnouncementRepository announcementRepo,
        IDeliverableSubmissionRepository submissionRepo,
        IDeliverableCommentRepository commentRepo,
        IDefenseScheduleRepository defenseRepo,
        IWebHostEnvironment environment)
    {
        _userManager = userManager;
        _projectRepo = projectRepo;
        _taskRepo = taskRepo;
        _auditRepo = auditRepo;
        _announcementRepo = announcementRepo;
        _submissionRepo = submissionRepo;
        _commentRepo = commentRepo;
        _defenseRepo = defenseRepo;
        _environment = environment;
    }

    public async Task<IActionResult> Index()
    {
        var me = await _userManager.GetUserAsync(User);
        var projects = await _projectRepo.GetByStudentAsync(me?.Id ?? "");
        
        var allTasks = new List<ProjectTask>();
        foreach(var p in projects) {
            allTasks.AddRange(await _taskRepo.GetByProjectAsync(p.Id));
        }

        var stats = new DashboardStatsViewModel
        {
            TotalProjects = projects.Count,
            TotalTasks = allTasks.Count,
            TasksTodo = allTasks.Count(t => t.Status == "Pending" || t.Status == "Needs Revision"),
            TasksInProgress = allTasks.Count(t => t.Status == "Submitted" || t.Status == "Under Review"),
            TasksDone = allTasks.Count(t => t.Status == "Approved"),
            RecentLogs = await _auditRepo.GetAllAsync(5)
        };
        return View(stats);
    }

    // ─── My Deliverables ──────────────────────────────────────
    public async Task<IActionResult> MyDeliverables()
    {
        var me = await _userManager.GetUserAsync(User);
        var studentProjects = await _projectRepo.GetByStudentAsync(me?.Id ?? "");
        var allTasks = new List<ProjectTaskViewModel>();

        foreach (var project in studentProjects)
        {
            var tasks = await _taskRepo.GetByProjectAsync(project.Id);
            var orderedTasks = tasks.OrderBy(t => t.OrderIndex).ToList();

            foreach (var t in orderedTasks)
            {
                // Check if previous deliverables are all approved (sequential gating)
                bool isLocked = false;
                if (t.OrderIndex > 0)
                {
                    var previousTasks = orderedTasks.Where(pt => pt.OrderIndex < t.OrderIndex).ToList();
                    isLocked = previousTasks.Any(pt => pt.Status != "Approved");
                }

                allTasks.Add(new ProjectTaskViewModel
                {
                    Id = t.Id, Title = t.Title, Description = t.Description,
                    Priority = t.Priority, Status = t.Status, DueDate = t.DueDate,
                    ProjectId = t.ProjectId, ProjectName = project.ProjectName,
                    Type = t.Type, OrderIndex = t.OrderIndex,
                    AttachmentPath = t.AttachmentPath, AttachmentFileName = t.AttachmentFileName,
                    IsLocked = isLocked
                });
            }
        }

        return View(allTasks);
    }

    [HttpGet]
    public async Task<IActionResult> GetDeliverableDetails(int id)
    {
        var t = await _taskRepo.GetByIdAsync(id);
        if (t == null) return NotFound();
        
        var me = await _userManager.GetUserAsync(User);
        var submission = await _submissionRepo.GetLatestByStudentAndTaskAsync(me?.Id ?? "", id);
        var comments = await _commentRepo.GetByTaskAsync(id);

        return Json(new { 
            t.Id, t.Title, t.Description, t.Status, t.Type, DueDate = t.DueDate?.ToString("MMM dd, yyyy"),
            t.AttachmentPath, t.AttachmentFileName, t.OrderIndex,
            Submission = submission != null ? new { submission.SubmissionNote, submission.FilePath, submission.OriginalFileName, SubmittedAt = submission.SubmittedAt.ToString("MMM dd, yyyy HH:mm") } : null,
            Comments = comments.Select(c => new { c.Author?.FullName, c.Content, CreatedAt = c.CreatedAt.ToString("MMM dd, yyyy HH:mm") })
        });
    }

    [HttpPost][ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitDeliverable(int taskId, string note, IFormFile? file)
    {
        var me = await _userManager.GetUserAsync(User);
        var task = await _taskRepo.GetByIdAsync(taskId);
        if (task == null) return Json(new { success = false, message = "Task not found." });

        // ─── Sequential Deliverable Enforcement ──────────────
        if (task.OrderIndex > 0)
        {
            var projectTasks = await _taskRepo.GetByProjectAsync(task.ProjectId);
            var previousTasks = projectTasks.Where(t => t.OrderIndex < task.OrderIndex).ToList();
            if (previousTasks.Any(t => t.Status != "Approved"))
            {
                return Json(new { success = false, message = "You must complete all previous deliverables before submitting this one. Ensure earlier deliverables are approved first." });
            }
        }

        var submission = new DeliverableSubmission { ProjectTaskId = taskId, StudentId = me?.Id ?? "", SubmissionNote = note };

        if (file != null)
        {
            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            var path = Path.Combine(_environment.WebRootPath, "uploads", "submissions");
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);

            using (var stream = new FileStream(Path.Combine(path, fileName), FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            submission.FilePath = "/uploads/submissions/" + fileName;
            submission.OriginalFileName = file.FileName;
        }

        await _submissionRepo.AddAsync(submission);
        
        task.Status = "Submitted";
        await _taskRepo.UpdateAsync(task);

        await _auditRepo.AddAsync(new AuditLog { UserId = me?.Id, Action = $"Student submitted deliverable '{task.Title}'", Timestamp = DateTime.UtcNow });
        return Json(new { success = true, message = "Submission recorded." });
    }

    // ─── Announcements ────────────────────────────────────────
    public async Task<IActionResult> Announcements()
    {
        var me = await _userManager.GetUserAsync(User);
        var announcements = await _announcementRepo.GetAllForStudentAsync(me?.Id ?? "");
        return View(announcements);
    }

    // ─── Archive ──────────────────────────────────────────────
    public async Task<IActionResult> Archive()
    {
        var me = await _userManager.GetUserAsync(User);
        var studentProjects = await _projectRepo.GetByStudentAsync(me?.Id ?? "");
        var archivedTasks = new List<ProjectTaskViewModel>();

        foreach (var project in studentProjects)
        {
            var archived = await _taskRepo.GetArchivedByProjectAsync(project.Id);
            archivedTasks.AddRange(archived.Select(t => new ProjectTaskViewModel
            {
                Id = t.Id, Title = t.Title, Description = t.Description,
                Priority = t.Priority, Status = t.Status, DueDate = t.DueDate,
                ProjectId = t.ProjectId, ProjectName = project.ProjectName,
                Type = t.Type, OrderIndex = t.OrderIndex
            }));
        }

        return View(archivedTasks);
    }

    // ─── Defense Schedule ─────────────────────────────────────
    public async Task<IActionResult> DefenseSchedule()
    {
        var me = await _userManager.GetUserAsync(User);
        var schedules = await _defenseRepo.GetByStudentAsync(me?.Id ?? "");
        return View(schedules);
    }
}
