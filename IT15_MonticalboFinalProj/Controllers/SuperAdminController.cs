using IT15_MonticalboFinalProj.Data;
using IT15_MonticalboFinalProj.Models;
using IT15_MonticalboFinalProj.Models.ViewModels;
using IT15_MonticalboFinalProj.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IT15_MonticalboFinalProj.Controllers;

[Authorize(Roles = "SuperAdmin")]
public class SuperAdminController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserRepository _userRepo;
    private readonly IProjectRepository _projectRepo;
    private readonly IAuditLogRepository _auditRepo;
    private readonly IProjectTaskRepository _taskRepo;
    private readonly IDepartmentRepository _deptRepo;
    private readonly ApplicationDbContext _context;

    public SuperAdminController(
        UserManager<ApplicationUser> userManager,
        IUserRepository userRepo,
        IProjectRepository projectRepo,
        IAuditLogRepository auditRepo,
        IProjectTaskRepository taskRepo,
        IDepartmentRepository deptRepo,
        ApplicationDbContext context)
    {
        _userManager = userManager;
        _userRepo = userRepo;
        _projectRepo = projectRepo;
        _auditRepo = auditRepo;
        _taskRepo = taskRepo;
        _deptRepo = deptRepo;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var stats = new DashboardStatsViewModel
        {
            TotalAdmins = await _userRepo.CountByRoleAsync("Administrator"),
            TotalResearchAdvisers = await _userRepo.CountByRoleAsync("ResearchAdviser"),
            TotalStudents = await _userRepo.CountByRoleAsync("Student"),
            TotalUsers = await _userRepo.CountAsync(),
            TotalProjects = await _projectRepo.CountAsync(),
            TotalTasks = await _taskRepo.CountByStatusAsync("Todo") +
                         await _taskRepo.CountByStatusAsync("In Progress") +
                         await _taskRepo.CountByStatusAsync("Done"),
            TotalAuditLogs = await _auditRepo.CountAsync(),
            ActiveProjects = await _projectRepo.CountByStatusAsync("Active"),
            PendingProjects = await _projectRepo.CountByStatusAsync("Pending"),
            CompletedProjects = await _projectRepo.CountByStatusAsync("Completed"),
            RecentLogs = await _auditRepo.GetAllAsync(10)
        };
        var setting = await _context.SystemSettings.FirstOrDefaultAsync();
        stats.SystemName = setting?.SystemName ?? "ResearchHub";
        return View(stats);
    }

    // ─── Manage Users ─────────────────────────────────────────
    public async Task<IActionResult> ManageUsers()
    {
        var users = await _userRepo.GetAllAsync();
        var departments = await _deptRepo.GetAllAsync();

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
                DepartmentId = u.DepartmentId,
                DepartmentName = u.Department?.Name ?? "No Department"
            });
        }

        ViewBag.Departments = departments;
        return View(vms);
    }

    [HttpGet]
    public async Task<IActionResult> GetUserDetails(string id)
    {
        var user = await _userRepo.GetByIdAsync(id);
        if (user == null) return NotFound();
        var roles = await _userManager.GetRolesAsync(user);
        return Json(new { user.Id, user.FullName, user.Email, Username = user.UserName, user.Status, Role = roles.FirstOrDefault() });
    }

    [HttpPost][ValidateAntiForgeryToken]
    public async Task<IActionResult> AddUser(AdminUserViewModel model)
    {
        if (!ModelState.IsValid) return Json(new { success = false, message = "Validation failed." });
        if (await _userManager.FindByEmailAsync(model.Email) != null) return Json(new { success = false, message = "Email already exists." });
        if (await _userManager.FindByNameAsync(model.Username) != null) return Json(new { success = false, message = "Username already taken." });
        
        if (model.Role != "SuperAdmin" && (model.DepartmentId == null || model.DepartmentId == 0))
            return Json(new { success = false, message = "Please select a department for this role." });

        var user = new ApplicationUser
        {
            FullName = model.FullName, Email = model.Email,
            UserName = model.Username, EmailConfirmed = true,
            Status = "Active", CreatedAt = DateTime.UtcNow,
            DepartmentId = model.DepartmentId,
            IsActive = true,
            IsProfileComplete = true
        };
        var result = await _userManager.CreateAsync(user, model.Password ?? "Admin123!");
        if (!result.Succeeded) return Json(new { success = false, message = result.Errors.First().Description });

        // Role mapping
        var role = model.Role switch {
            "SuperAdmin" => "SuperAdmin",
            "Admin" => "Administrator",
            "Adviser" => "ResearchAdviser",
            _ => "Student"
        };
        await _userManager.AddToRoleAsync(user, role);

        var me = await _userManager.GetUserAsync(User);
        await _auditRepo.AddAsync(new AuditLog { UserId = me?.Id, Action = $"SuperAdmin created user '{user.UserName}' with role '{role}'", Timestamp = DateTime.UtcNow });
        return Json(new { success = true, message = "User created successfully." });
    }

    [HttpPost][ValidateAntiForgeryToken]
    public async Task<IActionResult> EditUser(AdminUserViewModel model)
    {
        var user = await _userRepo.GetByIdAsync(model.Id!);
        if (user == null) return Json(new { success = false, message = "User not found." });
        
        user.FullName = model.FullName; user.Email = model.Email; user.UserName = model.Username; 
        user.Status = model.Status; user.DepartmentId = model.DepartmentId;

        if (model.Role != "SuperAdmin" && (model.DepartmentId == null || model.DepartmentId == 0))
            return Json(new { success = false, message = "Please select a department for this role." });

        if (!string.IsNullOrEmpty(model.Password)) { var token = await _userManager.GeneratePasswordResetTokenAsync(user); await _userManager.ResetPasswordAsync(user, token, model.Password); }
        
        var currentRoles = await _userManager.GetRolesAsync(user);
        var targetRole = model.Role switch {
            "SuperAdmin" => "SuperAdmin",
            "Admin" => "Administrator",
            "Adviser" => "ResearchAdviser",
            _ => "Student"
        };
        if (!currentRoles.Contains(targetRole)) { await _userManager.RemoveFromRolesAsync(user, currentRoles); await _userManager.AddToRoleAsync(user, targetRole); }
        
        await _userRepo.UpdateAsync(user);
        var me = await _userManager.GetUserAsync(User);
        await _auditRepo.AddAsync(new AuditLog { UserId = me?.Id, Action = $"SuperAdmin updated user '{user.UserName}'", Timestamp = DateTime.UtcNow });
        return Json(new { success = true, message = "User updated successfully." });
    }

    [HttpPost][ValidateAntiForgeryToken]
    public async Task<IActionResult> ArchiveUser(string id)
    {
        var user = await _userRepo.GetByIdAsync(id);
        if (user == null) return Json(new { success = false, message = "User not found." });
        
        if (await _userManager.IsInRoleAsync(user, "SuperAdmin"))
            return Json(new { success = false, message = "SuperAdmin accounts cannot be deactivated." });

        await _userRepo.ArchiveAsync(id);
        var me = await _userManager.GetUserAsync(User);
        await _auditRepo.AddAsync(new AuditLog { UserId = me?.Id, Action = $"SuperAdmin archived user '{user.UserName}'", Timestamp = DateTime.UtcNow });
        return Json(new { success = true, message = "User archived." });
    }

    [HttpPost][ValidateAntiForgeryToken]
    public async Task<IActionResult> RestoreUser(string id)
    {
        var user = await _userRepo.GetByIdAsync(id);
        if (user == null) return Json(new { success = false, message = "User not found." });
        await _userRepo.RestoreAsync(id);
        var me = await _userManager.GetUserAsync(User);
        await _auditRepo.AddAsync(new AuditLog { UserId = me?.Id, Action = $"SuperAdmin restored user '{user.UserName}'", Timestamp = DateTime.UtcNow });
        return Json(new { success = true, message = "User restored." });
    }

    // ─── Manage Departments ──────────────────────────────────
    public async Task<IActionResult> ManageDepartments()
    {
        var departments = await _deptRepo.GetAllAsync();
        return View(departments);
    }

    [HttpPost][ValidateAntiForgeryToken]
    public async Task<IActionResult> AddDepartment(Department model)
    {
        if (!ModelState.IsValid) return Json(new { success = false, message = "Validation failed." });
        await _deptRepo.AddAsync(model);
        var me = await _userManager.GetUserAsync(User);
        await _auditRepo.AddAsync(new AuditLog { UserId = me?.Id, Action = $"SuperAdmin created department '{model.Name}'", Timestamp = DateTime.UtcNow });
        return Json(new { success = true, message = "Department created successfully." });
    }

    [HttpPost][ValidateAntiForgeryToken]
    public async Task<IActionResult> EditDepartment(Department model)
    {
        var dept = await _deptRepo.GetByIdAsync(model.Id);
        if (dept == null) return Json(new { success = false, message = "Department not found." });
        dept.Name = model.Name; dept.Description = model.Description;
        await _deptRepo.UpdateAsync(dept);
        var me = await _userManager.GetUserAsync(User);
        await _auditRepo.AddAsync(new AuditLog { UserId = me?.Id, Action = $"SuperAdmin updated department '{dept.Name}'", Timestamp = DateTime.UtcNow });
        return Json(new { success = true, message = "Department updated successfully." });
    }

    [HttpPost][ValidateAntiForgeryToken]
    public async Task<IActionResult> InactivateDepartment(int id)
    {
        var dept = await _deptRepo.GetByIdAsync(id);
        if (dept == null) return Json(new { success = false, message = "Department not found." });
        await _deptRepo.InactivateAsync(id);
        var me = await _userManager.GetUserAsync(User);
        await _auditRepo.AddAsync(new AuditLog { UserId = me?.Id, Action = $"SuperAdmin inactivated department '{dept.Name}'", Timestamp = DateTime.UtcNow });
        return Json(new { success = true, message = "Department and associated users inactivated." });
    }

    [HttpPost][ValidateAntiForgeryToken]
    public async Task<IActionResult> RestoreDepartment(int id)
    {
        var dept = await _deptRepo.GetByIdAsync(id);
        if (dept == null) return Json(new { success = false, message = "Department not found." });
        await _deptRepo.RestoreAsync(id);
        var me = await _userManager.GetUserAsync(User);
        await _auditRepo.AddAsync(new AuditLog { UserId = me?.Id, Action = $"SuperAdmin restored department '{dept.Name}'", Timestamp = DateTime.UtcNow });
        return Json(new { success = true, message = "Department restored." });
    }

    [HttpPost][ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUser(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return Json(new { success = false, message = "User not found." });
        
        var name = user.FullName;
        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded) return Json(new { success = false, message = "Failed to delete user." });

        var me = await _userManager.GetUserAsync(User);
        await _auditRepo.AddAsync(new AuditLog { UserId = me?.Id, Action = $"SuperAdmin hard deleted user '{name}'", Timestamp = DateTime.UtcNow });
        return Json(new { success = true, message = "User deleted permanently." });
    }

    [HttpPost][ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteDepartment(int id)
    {
        var dept = await _deptRepo.GetByIdAsync(id);
        if (dept == null) return Json(new { success = false, message = "Department not found." });

        var name = dept.Name;
        await _deptRepo.DeleteAsync(id);
        var me = await _userManager.GetUserAsync(User);
        await _auditRepo.AddAsync(new AuditLog { UserId = me?.Id, Action = $"SuperAdmin hard deleted department '{name}'", Timestamp = DateTime.UtcNow });
        return Json(new { success = true, message = "Department deleted permanently." });
    }

    // ─── Archive ──────────────────────────────────────────────
    public async Task<IActionResult> Archive()
    {
        var archivedUsers = await _userRepo.GetArchivedAsync();
        var archivedDepts = await _deptRepo.GetArchivedAsync();

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

        ViewBag.ArchivedDepartments = archivedDepts;
        return View(userVms);
    }

    // ─── System Settings ──────────────────────────────────────
    public async Task<IActionResult> SystemSettings()
    {
        var setting = await _context.SystemSettings.FirstOrDefaultAsync() ?? new SystemSetting();
        return View(setting);
    }

    [HttpPost][ValidateAntiForgeryToken]
    public async Task<IActionResult> SystemSettings(SystemSetting model)
    {
        var existing = await _context.SystemSettings.FirstOrDefaultAsync();
        if (existing == null) { _context.SystemSettings.Add(model); }
        else { existing.SystemName = model.SystemName; existing.Theme = model.Theme; }
        await _context.SaveChangesAsync();
        var me = await _userManager.GetUserAsync(User);
        await _auditRepo.AddAsync(new AuditLog { UserId = me?.Id, Action = "System settings updated", Timestamp = DateTime.UtcNow });
        TempData["SuccessMsg"] = "Settings saved successfully.";
        return RedirectToAction("SystemSettings");
    }

    public async Task<IActionResult> AuditLogs()
    {
        var logs = await _auditRepo.GetAllAsync(200);
        return View(logs);
    }
}
