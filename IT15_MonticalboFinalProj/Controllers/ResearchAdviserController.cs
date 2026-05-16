using IT15_MonticalboFinalProj.Models;
using IT15_MonticalboFinalProj.Models.ViewModels;
using IT15_MonticalboFinalProj.Repositories;
using IT15_MonticalboFinalProj.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IT15_MonticalboFinalProj.Controllers;

[Authorize(Roles = "ResearchAdviser")]
public class ResearchAdviserController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IProjectRepository _projectRepo;
    private readonly IProjectTaskRepository _taskRepo;
    private readonly IAuditLogRepository _auditRepo;
    private readonly IUserRepository _userRepo;
    private readonly IProjectMemberRepository _memberRepo;
    private readonly IAnnouncementRepository _announcementRepo;
    private readonly IDeliverableSubmissionRepository _submissionRepo;
    private readonly IDeliverableCommentRepository _commentRepo;
    private readonly IDefenseScheduleRepository _defenseRepo;
    private readonly IWebHostEnvironment _environment;
    private readonly IEmailService _emailService;

    public ResearchAdviserController(
        UserManager<ApplicationUser> userManager,
        IProjectRepository projectRepo,
        IProjectTaskRepository taskRepo,
        IAuditLogRepository auditRepo,
        IUserRepository userRepo,
        IProjectMemberRepository memberRepo,
        IAnnouncementRepository announcementRepo,
        IDeliverableSubmissionRepository submissionRepo,
        IDeliverableCommentRepository commentRepo,
        IDefenseScheduleRepository defenseRepo,
        IWebHostEnvironment environment,
        IEmailService emailService)
    {
        _userManager = userManager;
        _projectRepo = projectRepo;
        _taskRepo = taskRepo;
        _auditRepo = auditRepo;
        _userRepo = userRepo;
        _memberRepo = memberRepo;
        _announcementRepo = announcementRepo;
        _submissionRepo = submissionRepo;
        _commentRepo = commentRepo;
        _defenseRepo = defenseRepo;
        _environment = environment;
        _emailService = emailService;
    }

    public async Task<IActionResult> Index()
    {
        var stats = new DashboardStatsViewModel
        {
            TotalProjects = await _projectRepo.CountAsync(),
            ActiveProjects = await _projectRepo.CountByStatusAsync("Active"),
            PendingProjects = await _projectRepo.CountByStatusAsync("Pending"),
            CompletedProjects = await _projectRepo.CountByStatusAsync("Completed"),
            TotalTasks = await _taskRepo.CountByStatusAsync("Todo") +
                         await _taskRepo.CountByStatusAsync("In Progress") +
                         await _taskRepo.CountByStatusAsync("Done"),
            TasksTodo = await _taskRepo.CountByStatusAsync("Todo"),
            TasksInProgress = await _taskRepo.CountByStatusAsync("In Progress"),
            TasksDone = await _taskRepo.CountByStatusAsync("Done"),
            RecentLogs = await _auditRepo.GetAllAsync(5)
        };
        return View(stats);
    }

    // ─── My Research Groups ───────────────────────────────────
    public async Task<IActionResult> MyResearchGroups()
    {
        var me = await _userManager.GetUserAsync(User);
        var projects = await _projectRepo.GetByAdviserAsync(me?.Id ?? "");
        
        var vms = projects.Select(p => new ProjectViewModel
        {
            Id = p.Id, ProjectName = p.ProjectName, Description = p.Description,
            StartDate = p.StartDate, EndDate = p.EndDate, Status = p.Status,
            CreatedByName = p.CreatedBy?.FullName, CreatedAt = p.CreatedAt,
            AdviserId = p.AdviserId, AdviserName = p.Adviser?.FullName,
            IsAcceptedByAdviser = p.IsAcceptedByAdviser
        }).ToList();

        return View(vms);
    }

    [HttpPost][ValidateAntiForgeryToken]
    public async Task<IActionResult> AcceptGroup(int id)
    {
        var p = await _projectRepo.GetByIdAsync(id);
        var me = await _userManager.GetUserAsync(User);
        if (p == null || p.AdviserId != me?.Id) return Json(new { success = false, message = "Not authorized." });

        p.IsAcceptedByAdviser = true;
        await _projectRepo.UpdateAsync(p);
        
        await _auditRepo.AddAsync(new AuditLog { UserId = me?.Id, Action = $"Adviser accepted research group '{p.ProjectName}'", Timestamp = DateTime.UtcNow });
        return Json(new { success = true });
    }

    [HttpGet]
    public async Task<IActionResult> GetProjectDetails(int id)
    {
        var p = await _projectRepo.GetByIdAsync(id);
        if (p == null) return NotFound();
        return Json(new { p.Id, p.ProjectName, p.Description, StartDate = p.StartDate.ToString("yyyy-MM-dd"), EndDate = p.EndDate?.ToString("yyyy-MM-dd"), p.Status });
    }

    [HttpPost][ValidateAntiForgeryToken]
    public async Task<IActionResult> AddProject(ProjectViewModel model)
    {
        if (!ModelState.IsValid) return Json(new { success = false, message = "Validation failed." });
        var me = await _userManager.GetUserAsync(User);
        var project = new Project { ProjectName = model.ProjectName, Description = model.Description, StartDate = model.StartDate, EndDate = model.EndDate, Status = model.Status, CreatedById = me?.Id, CreatedAt = DateTime.UtcNow };
        await _projectRepo.AddAsync(project);
        await _auditRepo.AddAsync(new AuditLog { UserId = me?.Id, Action = $"Adviser created research group '{project.ProjectName}'", Timestamp = DateTime.UtcNow });
        return Json(new { success = true, message = "Research group created." });
    }

    [HttpPost][ValidateAntiForgeryToken]
    public async Task<IActionResult> EditProject(ProjectViewModel model)
    {
        var project = await _projectRepo.GetByIdAsync(model.Id);
        if (project == null) return Json(new { success = false, message = "Research group not found." });
        project.ProjectName = model.ProjectName; project.Description = model.Description;
        project.StartDate = model.StartDate; project.EndDate = model.EndDate; project.Status = model.Status;
        await _projectRepo.UpdateAsync(project);
        var me = await _userManager.GetUserAsync(User);
        await _auditRepo.AddAsync(new AuditLog { UserId = me?.Id, Action = $"Adviser updated research group '{project.ProjectName}'", Timestamp = DateTime.UtcNow });
        return Json(new { success = true, message = "Research group updated." });
    }

    [HttpPost][ValidateAntiForgeryToken]
    public async Task<IActionResult> ArchiveProject(int id)
    {
        var project = await _projectRepo.GetByIdAsync(id);
        if (project == null) return Json(new { success = false, message = "Research group not found." });
        var name = project.ProjectName;
        await _projectRepo.ArchiveAsync(id);
        var me = await _userManager.GetUserAsync(User);
        await _auditRepo.AddAsync(new AuditLog { UserId = me?.Id, Action = $"Adviser archived research group '{name}'", Timestamp = DateTime.UtcNow });
        return Json(new { success = true, message = "Research group archived." });
    }

    [HttpPost][ValidateAntiForgeryToken]
    public async Task<IActionResult> RestoreProject(int id)
    {
        var project = await _projectRepo.GetByIdAsync(id);
        if (project == null) return Json(new { success = false, message = "Research group not found." });
        var name = project.ProjectName;
        await _projectRepo.RestoreAsync(id);
        var me = await _userManager.GetUserAsync(User);
        await _auditRepo.AddAsync(new AuditLog { UserId = me?.Id, Action = $"Adviser restored research group '{name}'", Timestamp = DateTime.UtcNow });
        return Json(new { success = true, message = "Research group restored." });
    }

    // ─── Deliverables ─────────────────────────────────────────
    public async Task<IActionResult> ManageDeliverables(int projectId)
    {
        var project = await _projectRepo.GetByIdAsync(projectId);
        var me = await _userManager.GetUserAsync(User);
        if (project == null || project.AdviserId != me?.Id) return Unauthorized();

        var tasks = await _taskRepo.GetByProjectAsync(projectId);
        var archived = await _taskRepo.GetArchivedByProjectAsync(projectId);
        
        // Members for assignment
        var members = await _memberRepo.GetByProjectAsync(projectId);

        var vms = tasks.Select(t => new ProjectTaskViewModel
        {
            Id = t.Id, Title = t.Title, Description = t.Description,
            Priority = t.Priority, Status = t.Status, DueDate = t.DueDate,
            ProjectId = t.ProjectId, ProjectName = project.ProjectName,
            AssignedToId = t.AssignedToId, AssignedToName = t.AssignedTo?.FullName, CreatedAt = t.CreatedAt,
            Type = t.Type, OrderIndex = t.OrderIndex,
            AttachmentPath = t.AttachmentPath, AttachmentFileName = t.AttachmentFileName
        }).ToList();

        ViewBag.Project = project;
        ViewBag.Members = members;
        ViewBag.ArchivedDeliverables = archived;
        return View(vms);
    }

    [HttpGet]
    public async Task<IActionResult> GetSubmissionDetails(int taskId)
    {
        var submissions = await _submissionRepo.GetByTaskAsync(taskId);
        var comments = await _commentRepo.GetByTaskAsync(taskId);
        
        return Json(new { 
            submissions = submissions.Select(s => new { s.Student?.FullName, s.SubmissionNote, s.FilePath, s.OriginalFileName, SubmittedAt = s.SubmittedAt.ToString("MMM dd, yyyy HH:mm") }),
            comments = comments.Select(c => new { c.Author?.FullName, c.Content, CreatedAt = c.CreatedAt.ToString("MMM dd, yyyy HH:mm") })
        });
    }

    [HttpPost][ValidateAntiForgeryToken]
    public async Task<IActionResult> AddFeedback(int taskId, string content, string? newStatus)
    {
        var me = await _userManager.GetUserAsync(User);
        var comment = new DeliverableComment { ProjectTaskId = taskId, AuthorId = me?.Id ?? "", Content = content };
        await _commentRepo.AddAsync(comment);

        if (!string.IsNullOrEmpty(newStatus))
        {
            var task = await _taskRepo.GetByIdAsync(taskId);
            if (task != null)
            {
                task.Status = newStatus;
                await _taskRepo.UpdateAsync(task);
            }
        }

        return Json(new { success = true, message = "Feedback and status updated successfully." });
    }

    // ─── Announcements ────────────────────────────────────────
    public async Task<IActionResult> ManageAnnouncements(int projectId)
    {
        var p = await _projectRepo.GetByIdAsync(projectId);
        if (p == null) return NotFound();
        
        var announcements = await _announcementRepo.GetByProjectAsync(projectId);
        ViewBag.Project = p;
        return View(announcements);
    }

    [HttpPost][ValidateAntiForgeryToken]
    public async Task<IActionResult> AddAnnouncement(int projectId, string title, string content)
    {
        var me = await _userManager.GetUserAsync(User);
        var a = new Announcement { ProjectId = projectId, AuthorId = me?.Id ?? "", Title = title, Content = content };
        await _announcementRepo.AddAsync(a);

        // Send Email Notifications to Students in the project
        var members = await _memberRepo.GetByProjectAsync(projectId);
        foreach (var m in members)
        {
            if (m.Student != null && !string.IsNullOrEmpty(m.Student.Email))
            {
                string subject = $"New Research Announcement: {title}";
                string body = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #e0e0e0; border-radius: 8px; overflow: hidden;'>
                        <div style='background-color: #4F46E5; color: #ffffff; padding: 20px; text-align: center;'>
                            <h1 style='margin: 0; font-size: 24px;'>New Research Announcement</h1>
                        </div>
                        <div style='padding: 30px; line-height: 1.6; color: #333333;'>
                            <p>Hello <strong>{m.Student.FullName}</strong>,</p>
                            <p>Your Research Adviser, <strong>{me?.FullName}</strong>, has posted a new announcement for your project: <strong>{title}</strong></p>
                            <div style='background-color: #f9fafb; border-left: 4px solid #4F46E5; padding: 15px; margin: 20px 0;'>
                                {content}
                            </div>
                            <p style='margin-top: 30px;'>Please log in to the Projexis portal to view the full details.</p>
                            <a href='#' style='display: inline-block; background-color: #4F46E5; color: #ffffff; text-decoration: none; padding: 12px 25px; border-radius: 5px; font-weight: bold; margin-top: 10px;'>Open Projexis Portal</a>
                        </div>
                        <div style='background-color: #f3f4f6; color: #6b7280; padding: 15px; text-align: center; font-size: 12px;'>
                            &copy; {DateTime.Now.Year} Projexis Research Management System
                        </div>
                    </div>";
                await _emailService.SendEmailAsync(m.Student.Email, subject, body);
            }
        }

        await _auditRepo.AddAsync(new AuditLog { UserId = me?.Id, Action = $"Adviser posted announcement for project #{projectId}", Timestamp = DateTime.UtcNow });
        return Json(new { success = true, message = "Announcement posted and students notified via email." });
    }

    [HttpGet]
    public async Task<IActionResult> GetDeliverableDetails(int id)
    {
        var t = await _taskRepo.GetByIdAsync(id);
        if (t == null) return NotFound();
        return Json(new { t.Id, t.Title, t.Description, t.Priority, t.Status, DueDate = t.DueDate?.ToString("yyyy-MM-dd"), t.ProjectId, t.AssignedToId, t.Type, t.OrderIndex, t.AttachmentPath, t.AttachmentFileName });
    }

    [HttpPost][ValidateAntiForgeryToken]
    public async Task<IActionResult> AddDeliverable(ProjectTaskViewModel model, IFormFile? attachment)
    {
        if (!ModelState.IsValid) return Json(new { success = false, message = "Validation failed." });

        // Server-side: due date must be in the future
        if (model.DueDate.HasValue && model.DueDate.Value.Date < DateTime.Today)
            return Json(new { success = false, message = "Due date cannot be in the past." });

        var me = await _userManager.GetUserAsync(User);
        var task = new ProjectTask
        {
            Title = model.Title, Description = model.Description, Priority = model.Priority,
            Status = "Pending", DueDate = model.DueDate, ProjectId = model.ProjectId,
            AssignedToId = string.IsNullOrEmpty(model.AssignedToId) ? null : model.AssignedToId,
            CreatedById = me?.Id, CreatedAt = DateTime.UtcNow,
            Type = model.Type, OrderIndex = model.OrderIndex
        };

        // Handle file attachment
        if (attachment != null && attachment.Length > 0)
        {
            var ext = Path.GetExtension(attachment.FileName).ToLowerInvariant();
            var allowed = new[] { ".pdf", ".doc", ".docx" };
            if (!allowed.Contains(ext))
                return Json(new { success = false, message = "Only PDF and Word files are allowed." });

            var fileName = Guid.NewGuid().ToString() + ext;
            var uploadPath = Path.Combine(_environment.WebRootPath, "uploads", "deliverables");
            if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);

            using (var stream = new FileStream(Path.Combine(uploadPath, fileName), FileMode.Create))
            {
                await attachment.CopyToAsync(stream);
            }
            task.AttachmentPath = "/uploads/deliverables/" + fileName;
            task.AttachmentFileName = attachment.FileName;
        }

        await _taskRepo.AddAsync(task);
        await _auditRepo.AddAsync(new AuditLog { UserId = me?.Id, Action = $"Adviser created deliverable '{task.Title}' in group #{task.ProjectId}", Timestamp = DateTime.UtcNow });
        return Json(new { success = true, message = "Deliverable created." });
    }

    [HttpPost][ValidateAntiForgeryToken]
    public async Task<IActionResult> EditDeliverable(ProjectTaskViewModel model, IFormFile? attachment)
    {
        var task = await _taskRepo.GetByIdAsync(model.Id);
        if (task == null) return Json(new { success = false, message = "Deliverable not found." });

        // Server-side: due date must be in the future
        if (model.DueDate.HasValue && model.DueDate.Value.Date < DateTime.Today)
            return Json(new { success = false, message = "Due date cannot be in the past." });

        task.Title = model.Title; task.Description = model.Description; task.Priority = model.Priority;
        task.Status = model.Status; task.DueDate = model.DueDate;
        task.AssignedToId = string.IsNullOrEmpty(model.AssignedToId) ? null : model.AssignedToId;
        task.Type = model.Type; task.OrderIndex = model.OrderIndex;

        // Handle new file attachment
        if (attachment != null && attachment.Length > 0)
        {
            var ext = Path.GetExtension(attachment.FileName).ToLowerInvariant();
            var allowed = new[] { ".pdf", ".doc", ".docx" };
            if (!allowed.Contains(ext))
                return Json(new { success = false, message = "Only PDF and Word files are allowed." });

            var fileName = Guid.NewGuid().ToString() + ext;
            var uploadPath = Path.Combine(_environment.WebRootPath, "uploads", "deliverables");
            if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);

            using (var stream = new FileStream(Path.Combine(uploadPath, fileName), FileMode.Create))
            {
                await attachment.CopyToAsync(stream);
            }
            task.AttachmentPath = "/uploads/deliverables/" + fileName;
            task.AttachmentFileName = attachment.FileName;
        }

        await _taskRepo.UpdateAsync(task);
        var me = await _userManager.GetUserAsync(User);
        await _auditRepo.AddAsync(new AuditLog { UserId = me?.Id, Action = $"Adviser updated deliverable '{task.Title}'", Timestamp = DateTime.UtcNow });
        return Json(new { success = true, message = "Deliverable updated." });
    }

    [HttpPost][ValidateAntiForgeryToken]
    public async Task<IActionResult> ArchiveDeliverable(int id)
    {
        var task = await _taskRepo.GetByIdAsync(id);
        if (task == null) return Json(new { success = false, message = "Deliverable not found." });
        var name = task.Title;
        await _taskRepo.ArchiveAsync(id);
        var me = await _userManager.GetUserAsync(User);
        await _auditRepo.AddAsync(new AuditLog { UserId = me?.Id, Action = $"Adviser archived deliverable '{name}'", Timestamp = DateTime.UtcNow });
        return Json(new { success = true, message = "Deliverable archived." });
    }

    [HttpPost][ValidateAntiForgeryToken]
    public async Task<IActionResult> RestoreDeliverable(int id)
    {
        var task = await _taskRepo.GetByIdAsync(id);
        if (task == null) return Json(new { success = false, message = "Deliverable not found." });
        var name = task.Title;
        await _taskRepo.RestoreAsync(id);
        var me = await _userManager.GetUserAsync(User);
        await _auditRepo.AddAsync(new AuditLog { UserId = me?.Id, Action = $"Adviser restored deliverable '{name}'", Timestamp = DateTime.UtcNow });
        return Json(new { success = true, message = "Deliverable restored." });
    }

    // ─── Archive ──────────────────────────────────────────────
    public async Task<IActionResult> Archive()
    {
        var me = await _userManager.GetUserAsync(User);
        var archivedProjects = await _projectRepo.GetArchivedAsync();
        var myArchivedProjects = archivedProjects.Where(p => p.AdviserId == me?.Id).ToList();

        var archivedProjectVms = myArchivedProjects.Select(p => new ProjectViewModel
        {
            Id = p.Id, ProjectName = p.ProjectName, Description = p.Description,
            StartDate = p.StartDate, EndDate = p.EndDate, Status = p.Status,
            CreatedByName = p.CreatedBy?.FullName, CreatedAt = p.CreatedAt, ArchivedAt = p.ArchivedAt
        }).ToList();

        var archivedDeliverables = await _taskRepo.GetArchivedByAdviserAsync(me?.Id ?? "");

        ViewBag.ArchivedProjects = archivedProjectVms;
        ViewBag.ArchivedDeliverables = archivedDeliverables;
        return View();
    }

    // ─── Group Submissions Tracker ────────────────────────────
    public async Task<IActionResult> GroupSubmissions(int projectId)
    {
        var project = await _projectRepo.GetByIdAsync(projectId);
        var me = await _userManager.GetUserAsync(User);
        if (project == null || project.AdviserId != me?.Id) return Unauthorized();

        var tasks = await _taskRepo.GetByProjectAsync(projectId);
        var vms = new List<ProjectTaskViewModel>();

        foreach (var t in tasks.OrderBy(t => t.OrderIndex))
        {
            var submissions = await _submissionRepo.GetByTaskAsync(t.Id);
            vms.Add(new ProjectTaskViewModel
            {
                Id = t.Id, Title = t.Title, Description = t.Description,
                Priority = t.Priority, Status = t.Status, DueDate = t.DueDate,
                ProjectId = t.ProjectId, ProjectName = project.ProjectName,
                Type = t.Type, OrderIndex = t.OrderIndex,
                AttachmentPath = t.AttachmentPath, AttachmentFileName = t.AttachmentFileName
            });
        }

        // Check if all deliverables approved AND group has 3 students → eligible for defense
        var members = await _memberRepo.GetByProjectAsync(projectId);
        bool allApproved = tasks.Any() && tasks.All(t => t.Status == "Approved") && members.Count == 3;
        
        // Check if defense is completed
        var schedules = await _defenseRepo.GetByProjectAsync(projectId);
        bool defenseCompleted = schedules.Any(s => s.Status == "Completed");

        ViewBag.Project = project;
        ViewBag.Members = members;
        ViewBag.AllDeliverablesApproved = allApproved;
        ViewBag.DefenseCompleted = defenseCompleted;
        ViewBag.MemberCount = members.Count;
        return View(vms);
    }

    // ─── Defense Schedules ────────────────────────────────────
    public async Task<IActionResult> DefenseSchedules()
    {
        var me = await _userManager.GetUserAsync(User);
        var schedules = await _defenseRepo.GetByAdviserAsync(me?.Id ?? "");
        var availability = await _defenseRepo.GetAvailabilityByAdviserAsync(me?.Id ?? "");

        ViewBag.Availability = availability;
        return View(schedules);
    }

    [HttpPost][ValidateAntiForgeryToken]
    public async Task<IActionResult> SetAvailability(DateTime availableDate, string startTime, string endTime)
    {
        if (availableDate.Date < DateTime.Today)
            return Json(new { success = false, message = "Cannot set availability for past dates." });

        var me = await _userManager.GetUserAsync(User);
        var slot = new AdviserAvailability
        {
            AdviserId = me?.Id ?? "",
            AvailableDate = availableDate,
            StartTime = TimeSpan.Parse(startTime),
            EndTime = TimeSpan.Parse(endTime)
        };
        await _defenseRepo.AddAvailabilityAsync(slot);
        return Json(new { success = true, message = "Availability slot added." });
    }

    [HttpPost][ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveAvailability(int id)
    {
        await _defenseRepo.RemoveAvailabilityAsync(id);
        return Json(new { success = true, message = "Availability slot removed." });
    }

    [HttpPost][ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveDefense(int id)
    {
        var schedule = await _defenseRepo.GetByIdAsync(id);
        var me = await _userManager.GetUserAsync(User);
        if (schedule == null || schedule.Project?.AdviserId != me?.Id)
            return Json(new { success = false, message = "Not authorized." });

        schedule.Status = "Approved by Adviser";
        schedule.ApprovedByAdviserId = me?.Id;
        await _defenseRepo.UpdateAsync(schedule);
        await _auditRepo.AddAsync(new AuditLog { UserId = me?.Id, Action = $"Adviser approved defense for '{schedule.Project?.ProjectName}'", Timestamp = DateTime.UtcNow });
        return Json(new { success = true, message = "Defense schedule approved." });
    }

    [HttpPost][ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectDefense(int id, string? remarks)
    {
        var schedule = await _defenseRepo.GetByIdAsync(id);
        var me = await _userManager.GetUserAsync(User);
        if (schedule == null || schedule.Project?.AdviserId != me?.Id)
            return Json(new { success = false, message = "Not authorized." });

        schedule.Status = "Cancelled";
        schedule.Remarks = remarks ?? "Rejected by adviser.";
        await _defenseRepo.UpdateAsync(schedule);
        await _auditRepo.AddAsync(new AuditLog { UserId = me?.Id, Action = $"Adviser rejected defense for '{schedule.Project?.ProjectName}'", Timestamp = DateTime.UtcNow });
        return Json(new { success = true, message = "Defense schedule rejected." });
    }

    [HttpPost][ValidateAntiForgeryToken]
    public async Task<IActionResult> CompleteDefense(int id, bool isPassed)
    {
        var schedule = await _defenseRepo.GetByIdAsync(id);
        var me = await _userManager.GetUserAsync(User);
        if (schedule == null || schedule.Project?.AdviserId != me?.Id)
            return Json(new { success = false, message = "Not authorized." });

        var project = schedule.Project;
        if (isPassed)
        {
            schedule.Status = "Defended";
            if (project != null)
            {
                project.Status = "Completed";
                project.CurrentStage = "Finished";
                project.ProgressPercentage = 100;
                await _projectRepo.UpdateAsync(project);
            }
            await _auditRepo.AddAsync(new AuditLog { UserId = me?.Id, Action = $"Adviser marked defense for {project?.ProjectName} as DEFENDED", Timestamp = DateTime.UtcNow });
        }
        else
        {
            schedule.Status = "Re-defense Required";
            if (project != null)
            {
                project.Status = "For Re-defense";
                project.CurrentStage = "Defense Preparation";
                await _projectRepo.UpdateAsync(project);
            }
            await _auditRepo.AddAsync(new AuditLog { UserId = me?.Id, Action = $"Adviser marked defense for {project?.ProjectName} as RE-DEFENSE REQUIRED", Timestamp = DateTime.UtcNow });
        }

        await _defenseRepo.UpdateAsync(schedule);
        return Json(new { success = true, message = isPassed ? "Defense marked as Defended. Project is now completed." : "Defense marked as Re-defense Required." });
    }

    [HttpPost][ValidateAntiForgeryToken]
    public async Task<IActionResult> FinalProjectSignOff(int projectId, string? remarks)
    {
        var project = await _projectRepo.GetByIdAsync(projectId);
        var me = await _userManager.GetUserAsync(User);
        if (project == null || project.AdviserId != me?.Id)
            return Json(new { success = false, message = "Not authorized." });

        project.Status = "Completed";
        project.CurrentStage = "Finished";
        project.ProgressPercentage = 100;
        await _projectRepo.UpdateAsync(project);

        await _auditRepo.AddAsync(new AuditLog { UserId = me?.Id, Action = $"Adviser gave FINAL SIGN-OFF for project {project.ProjectName}", Timestamp = DateTime.UtcNow });
        return Json(new { success = true, message = "Project successfully completed and moved to Research Repository!" });
    }

    [HttpPost][ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetDefenseStatus(int id)
    {
        var schedule = await _defenseRepo.GetByIdAsync(id);
        var me = await _userManager.GetUserAsync(User);
        if (schedule == null || schedule.Project?.AdviserId != me?.Id)
            return Json(new { success = false, message = "Not authorized." });

        schedule.Status = "Confirmed";
        await _defenseRepo.UpdateAsync(schedule);

        var project = schedule.Project;
        if (project != null)
        {
            project.Status = "For Defense";
            project.CurrentStage = "Defense Preparation";
            project.ProgressPercentage = 90; // Back to defense prep
            await _projectRepo.UpdateAsync(project);
        }

        await _auditRepo.AddAsync(new AuditLog { UserId = me?.Id, Action = $"Adviser reset defense status for {project?.ProjectName}", Timestamp = DateTime.UtcNow });
        return Json(new { success = true, message = "Defense status reset. You can now mark it again." });
    }
}
