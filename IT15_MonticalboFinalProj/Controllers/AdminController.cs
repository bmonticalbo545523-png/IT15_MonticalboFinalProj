using IT15_MonticalboFinalProj.Models;
using IT15_MonticalboFinalProj.Models.ViewModels;
using IT15_MonticalboFinalProj.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IT15_MonticalboFinalProj.Controllers;

[Authorize(Roles = "SuperAdmin,Administrator")]
public class AdminController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserRepository _userRepo;
    private readonly IProjectRepository _projectRepo;
    private readonly IAuditLogRepository _auditRepo;
    private readonly IProjectTaskRepository _taskRepo;
    private readonly IProjectMemberRepository _memberRepo;
    private readonly IDepartmentRepository _deptRepo;
    private readonly IDefenseScheduleRepository _defenseRepo;

    public AdminController(
        UserManager<ApplicationUser> userManager,
        IUserRepository userRepo,
        IProjectRepository projectRepo,
        IAuditLogRepository auditRepo,
        IProjectTaskRepository taskRepo,
        IProjectMemberRepository memberRepo,
        IDepartmentRepository deptRepo,
        IDefenseScheduleRepository defenseRepo)
    {
        _userManager = userManager;
        _userRepo = userRepo;
        _projectRepo = projectRepo;
        _auditRepo = auditRepo;
        _taskRepo = taskRepo;
        _memberRepo = memberRepo;
        _deptRepo = deptRepo;
        _defenseRepo = defenseRepo;
    }

    private async Task<int?> GetDeptId()
    {
        if (User.IsInRole("SuperAdmin")) return null;
        var user = await _userManager.GetUserAsync(User);
        return user?.DepartmentId;
    }
    public async Task<IActionResult> Index()
    {
        int? deptId = await GetDeptId();
        var stats = new DashboardStatsViewModel
        {
            TotalUsers = await _userRepo.CountAsync(deptId),
            TotalResearchAdvisers = await _userRepo.CountByRoleAsync("ResearchAdviser", deptId),
            TotalStudents = await _userRepo.CountByRoleAsync("Student", deptId),
            TotalProjects = await _projectRepo.CountAsync(deptId),
            ActiveProjects = await _projectRepo.CountByStatusAsync("Active", deptId),
            PendingProjects = await _projectRepo.CountByStatusAsync("Pending", deptId),
            CompletedProjects = await _projectRepo.CountByStatusAsync("Completed", deptId),
            TotalTasks = await _taskRepo.CountByStatusAsync("Todo", deptId) +
                         await _taskRepo.CountByStatusAsync("In Progress", deptId) +
                         await _taskRepo.CountByStatusAsync("Done", deptId),
            TasksTodo = await _taskRepo.CountByStatusAsync("Todo", deptId),
            TasksInProgress = await _taskRepo.CountByStatusAsync("In Progress", deptId),
            TasksDone = await _taskRepo.CountByStatusAsync("Done", deptId),
            TotalAuditLogs = await _auditRepo.CountAsync(),
            RecentLogs = await _auditRepo.GetAllAsync(5)
        };
        return View(stats);
    }

    // ─── Projects ─────────────────────────────────────────────
    public async Task<IActionResult> ManageProjects()
    {
        int? deptId = await GetDeptId();
        var projects = await _projectRepo.GetAllAsync(deptId);
        var archived = await _projectRepo.GetArchivedAsync(deptId);
        var advisers = await _userRepo.GetByRoleAsync("ResearchAdviser", deptId);
        var students = await _userRepo.GetByRoleAsync("Student", deptId);

        var vms = projects.Select(p => new ProjectViewModel
        {
            Id = p.Id, ProjectName = p.ProjectName, Description = p.Description,
            StartDate = p.StartDate, EndDate = p.EndDate, Status = p.Status,
            CreatedByName = p.CreatedBy?.FullName, CreatedAt = p.CreatedAt,
            AdviserId = p.AdviserId, AdviserName = p.Adviser?.FullName,
            IsAcceptedByAdviser = p.IsAcceptedByAdviser,
            MemberCount = p.Members.Count
        }).ToList();

        ViewBag.Advisers = advisers;
        ViewBag.Students = students;
        ViewBag.ArchivedProjects = archived;
        return View(vms);
    }

    [HttpGet]
    public async Task<IActionResult> GetProjectDetails(int id)
    {
        var p = await _projectRepo.GetByIdAsync(id);
        if (p == null) return NotFound();
        
        var members = await _memberRepo.GetByProjectAsync(id);
        
        return Json(new { 
            p.Id, p.ProjectName, p.Description, 
            StartDate = p.StartDate.ToString("yyyy-MM-dd"), 
            EndDate = p.EndDate?.ToString("yyyy-MM-dd"), 
            p.Status, p.AdviserId,
            Members = members.Select(m => new { m.StudentId, m.Student?.FullName })
        });
    }

    [HttpPost][ValidateAntiForgeryToken]
    public async Task<IActionResult> AddProject(ProjectViewModel model)
    {
        if (!ModelState.IsValid) return Json(new { success = false, message = "Validation failed." });
        var me = await _userManager.GetUserAsync(User);
        var project = new Project { 
            ProjectName = model.ProjectName, Description = model.Description, 
            StartDate = model.StartDate, EndDate = model.EndDate, 
            Status = model.Status, CreatedById = me?.Id, CreatedAt = DateTime.UtcNow,
            AdviserId = model.AdviserId,
            DepartmentId = me?.DepartmentId,
            CurrentStage = "Requirement Gathering"
        };
        await _projectRepo.AddAsync(project);
        await _auditRepo.AddAsync(new AuditLog { UserId = me?.Id, Action = $"Admin created research group '{project.ProjectName}'", Timestamp = DateTime.UtcNow });
        return Json(new { success = true, message = "Research group created." });
    }

    [HttpPost][ValidateAntiForgeryToken]
    public async Task<IActionResult> EditProject(ProjectViewModel model)
    {
        int? deptId = await GetDeptId();
        var project = await _projectRepo.GetByIdAsync(model.Id);
        if (project == null) return Json(new { success = false, message = "Project not found." });
        
        if (deptId != null && project.DepartmentId != deptId)
            return Json(new { success = false, message = "Access denied. Project belongs to another department." });

        project.ProjectName = model.ProjectName; project.Description = model.Description;
        project.StartDate = model.StartDate; project.EndDate = model.EndDate; 
        project.Status = model.Status; project.AdviserId = model.AdviserId;

        await _projectRepo.UpdateAsync(project);
        var me = await _userManager.GetUserAsync(User);
        await _auditRepo.AddAsync(new AuditLog { UserId = me?.Id, Action = $"Admin updated research group '{project.ProjectName}'", Timestamp = DateTime.UtcNow });
        return Json(new { success = true, message = "Research group updated." });
    }

    [HttpPost][ValidateAntiForgeryToken]
    public async Task<IActionResult> AddStudentToProject(int projectId, string studentId)
    {
        if (await _memberRepo.IsStudentInProjectAsync(projectId, studentId))
            return Json(new { success = false, message = "Student is already in this group." });

        var currentCount = await _memberRepo.GetCountByProjectAsync(projectId);
        if (currentCount >= 3)
            return Json(new { success = false, message = "This research group already has the maximum of 3 students." });

        var member = new ProjectMember { ProjectId = projectId, StudentId = studentId };
        await _memberRepo.AddAsync(member);
        
        return Json(new { success = true, message = "Student added to group." });
    }

    [HttpPost][ValidateAntiForgeryToken]
    public async Task<IActionResult> AddAllStudentsToProject(int projectId)
    {
        var currentCount = await _memberRepo.GetCountByProjectAsync(projectId);
        if (currentCount >= 3)
            return Json(new { success = false, message = "This research group already has the maximum of 3 students." });

        int? deptId = await GetDeptId();
        var allStudents = await _userRepo.GetByRoleAsync("Student", deptId);
        
        int added = 0;
        foreach (var s in allStudents)
        {
            if (currentCount >= 3) break;
            if (await _memberRepo.IsStudentInProjectAsync(projectId, s.Id)) continue;
            
            var member = new ProjectMember { ProjectId = projectId, StudentId = s.Id };
            await _memberRepo.AddAsync(member);
            currentCount++;
            added++;
        }

        if (added == 0) return Json(new { success = false, message = "No eligible students found to add." });
        return Json(new { success = true, message = $"Added {added} students to the group." });
    }

    [HttpPost][ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveStudentFromProject(int projectId, string studentId)
    {
        await _memberRepo.RemoveAsync(projectId, studentId);
        return Json(new { success = true, message = "Student removed from group." });
    }

    [HttpPost][ValidateAntiForgeryToken]
    public async Task<IActionResult> ArchiveProject(int id)
    {
        int? deptId = await GetDeptId();
        var project = await _projectRepo.GetByIdAsync(id);
        if (project == null) return Json(new { success = false, message = "Project not found." });
        
        if (deptId != null && project.DepartmentId != deptId)
            return Json(new { success = false, message = "Access denied." });

        var name = project.ProjectName;
        await _projectRepo.ArchiveAsync(id);
        var me = await _userManager.GetUserAsync(User);
        await _auditRepo.AddAsync(new AuditLog { UserId = me?.Id, Action = $"Admin archived research project '{name}'", Timestamp = DateTime.UtcNow });
        return Json(new { success = true, message = "Research project archived." });
    }

    [HttpPost][ValidateAntiForgeryToken]
    public async Task<IActionResult> RestoreProject(int id)
    {
        int? deptId = await GetDeptId();
        var project = await _projectRepo.GetByIdAsync(id);
        if (project == null) return Json(new { success = false, message = "Project not found." });

        if (deptId != null && project.DepartmentId != deptId)
            return Json(new { success = false, message = "Access denied." });

        var name = project.ProjectName;
        await _projectRepo.RestoreAsync(id);
        var me = await _userManager.GetUserAsync(User);
        await _auditRepo.AddAsync(new AuditLog { UserId = me?.Id, Action = $"Admin restored research project '{name}'", Timestamp = DateTime.UtcNow });
        return Json(new { success = true, message = "Research project restored." });
    }

    // ─── Analytics ────────────────────────────────────────────
    public async Task<IActionResult> Analytics()
    {
        int? deptId = await GetDeptId();
        var stats = new DashboardStatsViewModel
        {
            TotalProjects = await _projectRepo.CountAsync(deptId),
            ActiveProjects = await _projectRepo.CountByStatusAsync("Active", deptId),
            PendingProjects = await _projectRepo.CountByStatusAsync("Pending", deptId),
            CompletedProjects = await _projectRepo.CountByStatusAsync("Completed", deptId),
            TotalUsers = await _userRepo.CountAsync(deptId),
            TotalResearchAdvisers = await _userRepo.CountByRoleAsync("ResearchAdviser", deptId),
            TotalStudents = await _userRepo.CountByRoleAsync("Student", deptId),
            TotalTasks = await _taskRepo.CountByStatusAsync("Todo", deptId) + await _taskRepo.CountByStatusAsync("In Progress", deptId) + await _taskRepo.CountByStatusAsync("Done", deptId),
            TasksTodo = await _taskRepo.CountByStatusAsync("Todo", deptId),
            TasksInProgress = await _taskRepo.CountByStatusAsync("In Progress", deptId),
            TasksDone = await _taskRepo.CountByStatusAsync("Done", deptId)
        };
        return View(stats);
    }

    [HttpGet]
    public async Task<IActionResult> GetChartData()
    {
        int? deptId = await GetDeptId();
        return Json(new
        {
            projectLabels = new[] { "Active", "Pending", "Completed" },
            projectData = new[] { await _projectRepo.CountByStatusAsync("Active", deptId), await _projectRepo.CountByStatusAsync("Pending", deptId), await _projectRepo.CountByStatusAsync("Completed", deptId) },
            taskLabels = new[] { "Todo", "In Progress", "Done" },
            taskData = new[] { await _taskRepo.CountByStatusAsync("Todo", deptId), await _taskRepo.CountByStatusAsync("In Progress", deptId), await _taskRepo.CountByStatusAsync("Done", deptId) }
        });
    }

    // ─── Reports ──────────────────────────────────────────────
    public async Task<IActionResult> GenerateReport()
    {
        int? deptId = await GetDeptId();
        var projects = await _projectRepo.GetAllAsync(deptId);
        var users = await _userRepo.GetAllAsync(deptId);
        var me = await _userManager.GetUserAsync(User);

        ViewBag.Projects = projects;
        ViewBag.Users = users;
        ViewBag.GeneratedAt = DateTime.Now;
        ViewBag.PreparedBy = me?.FullName ?? "System Administrator";

        return View();
    }

    // ─── Manage Users ─────────────────────────────────────────
    public async Task<IActionResult> ManageUsers()
    {
        int? deptId = await GetDeptId();
        var users = await _userRepo.GetAllAsync(deptId);
        
        var vms = new List<AdminUserViewModel>();
        foreach (var u in users)
        {
            var roles = await _userManager.GetRolesAsync(u);
            vms.Add(new AdminUserViewModel
            {
                Id = u.Id, FullName = u.FullName,
                Email = u.Email ?? "", Username = u.UserName ?? "",
                Status = u.Status, CreatedAt = u.CreatedAt,
                Role = roles.FirstOrDefault() ?? "Student",
                DepartmentName = u.Department?.Name ?? "Your Department"
            });
        }
        return View(vms);
    }

    [HttpPost][ValidateAntiForgeryToken]
    public async Task<IActionResult> AddUser(AdminUserViewModel model)
    {
        int? deptId = await GetDeptId();
        if (await _userManager.FindByEmailAsync(model.Email) != null) return Json(new { success = false, message = "Email exists." });
        if (await _userManager.FindByNameAsync(model.Username) != null) return Json(new { success = false, message = "Username exists." });

        var user = new ApplicationUser
        {
            FullName = model.FullName, Email = model.Email,
            UserName = model.Username, EmailConfirmed = true,
            Status = "Active", CreatedAt = DateTime.UtcNow,
            DepartmentId = deptId,
            IsActive = true,
            IsProfileComplete = true
        };
        var result = await _userManager.CreateAsync(user, model.Password ?? "Admin123!");
        if (!result.Succeeded) return Json(new { success = false, message = result.Errors.First().Description });

        var role = model.Role == "Adviser" ? "ResearchAdviser" : "Student";
        await _userManager.AddToRoleAsync(user, role);

        var me = await _userManager.GetUserAsync(User);
        await _auditRepo.AddAsync(new AuditLog { UserId = me?.Id, Action = $"Admin created {role} '{user.UserName}'", Timestamp = DateTime.UtcNow });
        return Json(new { success = true, message = "User created." });
    }

    [HttpPost][ValidateAntiForgeryToken]
    public async Task<IActionResult> EditUser(AdminUserViewModel model)
    {
        int? deptId = await GetDeptId();
        var user = await _userRepo.GetByIdAsync(model.Id!);
        if (user == null) return Json(new { success = false, message = "Not found." });
        
        if (deptId != null && user.DepartmentId != deptId)
            return Json(new { success = false, message = "Access denied." });

        user.FullName = model.FullName; user.Email = model.Email; user.UserName = model.Username; 
        user.Status = model.Status;

        if (!string.IsNullOrEmpty(model.Password)) { 
            var t = await _userManager.GeneratePasswordResetTokenAsync(user); 
            await _userManager.ResetPasswordAsync(user, t, model.Password); 
        }
        
        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);
        await _userManager.AddToRoleAsync(user, model.Role == "Adviser" ? "ResearchAdviser" : "Student");

        await _userRepo.UpdateAsync(user);
        return Json(new { success = true, message = "User updated." });
    }

    [HttpPost][ValidateAntiForgeryToken]
    public async Task<IActionResult> ArchiveUser(string id)
    {
        int? deptId = await GetDeptId();
        var user = await _userRepo.GetByIdAsync(id);
        if (user == null) return Json(new { success = false, message = "User not found." });
        
        if (deptId != null && user.DepartmentId != deptId)
            return Json(new { success = false, message = "Access denied." });

        await _userRepo.ArchiveAsync(id);
        var me = await _userManager.GetUserAsync(User);
        await _auditRepo.AddAsync(new AuditLog { UserId = me?.Id, Action = $"Admin archived user '{user.UserName}'", Timestamp = DateTime.UtcNow });
        return Json(new { success = true, message = "User archived successfully." });
    }

    [HttpPost][ValidateAntiForgeryToken]
    public async Task<IActionResult> RestoreUser(string id)
    {
        int? deptId = await GetDeptId();
        var user = await _userRepo.GetByIdAsync(id);
        if (user == null) return Json(new { success = false, message = "User not found." });
        
        if (deptId != null && user.DepartmentId != deptId)
            return Json(new { success = false, message = "Access denied." });

        await _userRepo.RestoreAsync(id);
        var me = await _userManager.GetUserAsync(User);
        await _auditRepo.AddAsync(new AuditLog { UserId = me?.Id, Action = $"Admin restored user '{user.UserName}'", Timestamp = DateTime.UtcNow });
        return Json(new { success = true, message = "User restored successfully." });
    }

    // ─── Archive ──────────────────────────────────────────────
    public async Task<IActionResult> Archive()
    {
        int? deptId = await GetDeptId();
        var archivedUsers = await _userRepo.GetArchivedAsync(deptId);
        var archivedProjects = await _projectRepo.GetArchivedAsync(deptId);

        var userVms = new List<AdminUserViewModel>();
        foreach (var u in archivedUsers)
        {
            var roles = await _userManager.GetRolesAsync(u);
            userVms.Add(new AdminUserViewModel
            {
                Id = u.Id, FullName = u.FullName,
                Email = u.Email ?? "", Username = u.UserName ?? "",
                Status = u.Status, CreatedAt = u.CreatedAt,
                Role = roles.FirstOrDefault() ?? "Student",
                DepartmentName = u.Department?.Name ?? "No Department",
                ArchivedAt = u.ArchivedAt
            });
        }

        ViewBag.ArchivedProjects = archivedProjects;
        return View(userVms);
    }

    // ─── Defense Scheduling ───────────────────────────────────
    public async Task<IActionResult> DefenseScheduling()
    {
        int? deptId = await GetDeptId();
        var allSchedules = await _defenseRepo.GetAllAsync(deptId);
        
        // Get eligible groups (all deliverables approved)
        var projects = await _projectRepo.GetAllAsync(deptId);
        var eligibleProjects = new List<ProjectViewModel>();

        foreach (var p in projects)
        {
            var tasks = await _taskRepo.GetByProjectAsync(p.Id);
            var memberCount = await _memberRepo.GetCountByProjectAsync(p.Id);
            
            if (tasks.Any() && tasks.All(t => t.Status == "Approved") && memberCount == 3)
            {
                eligibleProjects.Add(new ProjectViewModel
                {
                    Id = p.Id, ProjectName = p.ProjectName,
                    AdviserId = p.AdviserId, AdviserName = p.Adviser?.FullName,
                    Status = p.Status
                });
            }
        }

        ViewBag.EligibleProjects = eligibleProjects;
        return View(allSchedules);
    }

    [HttpGet]
    public async Task<IActionResult> GetAdviserAvailability(string adviserId)
    {
        var slots = await _defenseRepo.GetAvailableSlotsByAdviserAsync(adviserId);
        return Json(slots.Select(s => new { 
            s.Id, AvailableDate = s.AvailableDate.ToString("yyyy-MM-dd"), 
            StartTime = s.StartTime.ToString(@"hh\:mm"), EndTime = s.EndTime.ToString(@"hh\:mm"),
            Display = $"{s.AvailableDate:MMM dd, yyyy} {s.StartTime:hh\\:mm} - {s.EndTime:hh\\:mm}"
        }));
    }

    [HttpPost][ValidateAntiForgeryToken]
    public async Task<IActionResult> ScheduleDefense(int projectId, int availabilityId, string location, string? remarks)
    {
        var slot = await _defenseRepo.GetAvailabilityByIdAsync(availabilityId);
        if (slot == null || slot.IsBooked) return Json(new { success = false, message = "Time slot is not available." });

        var project = await _projectRepo.GetByIdAsync(projectId);
        if (project == null) return Json(new { success = false, message = "Project not found." });

        // Verify eligibility - all deliverables must be approved and group must have 3 students
        var tasks = await _taskRepo.GetByProjectAsync(projectId);
        var memberCount = await _memberRepo.GetCountByProjectAsync(projectId);

        if (!tasks.Any() || !tasks.All(t => t.Status == "Approved"))
            return Json(new { success = false, message = "Not all deliverables are approved. Group is not eligible for defense." });

        if (memberCount != 3)
            return Json(new { success = false, message = "This research group does not have exactly 3 students. Group is not eligible for defense." });

        var me = await _userManager.GetUserAsync(User);
        var schedule = new DefenseSchedule
        {
            ProjectId = projectId,
            ScheduledDate = slot.AvailableDate,
            StartTime = slot.StartTime,
            EndTime = slot.EndTime,
            Location = location,
            ScheduledById = me?.Id,
            Status = "Pending",
            Remarks = remarks
        };

        await _defenseRepo.AddAsync(schedule);
        await _defenseRepo.BookSlotAsync(availabilityId);
        await _auditRepo.AddAsync(new AuditLog { UserId = me?.Id, Action = $"Admin scheduled defense for '{project.ProjectName}'", Timestamp = DateTime.UtcNow });
        return Json(new { success = true, message = "Defense scheduled successfully. Awaiting adviser approval." });
    }

    [HttpPost][ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmDefense(int id)
    {
        var schedule = await _defenseRepo.GetByIdAsync(id);
        if (schedule == null) return Json(new { success = false, message = "Schedule not found." });
        schedule.Status = "Confirmed";
        await _defenseRepo.UpdateAsync(schedule);
        var me = await _userManager.GetUserAsync(User);
        await _auditRepo.AddAsync(new AuditLog { UserId = me?.Id, Action = $"Admin confirmed defense for project #{schedule.ProjectId}", Timestamp = DateTime.UtcNow });
        return Json(new { success = true, message = "Defense confirmed." });
    }

    [HttpPost][ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelDefense(int id)
    {
        var schedule = await _defenseRepo.GetByIdAsync(id);
        if (schedule == null) return Json(new { success = false, message = "Schedule not found." });
        schedule.Status = "Cancelled";
        await _defenseRepo.UpdateAsync(schedule);
        return Json(new { success = true, message = "Defense cancelled." });
    }
}
