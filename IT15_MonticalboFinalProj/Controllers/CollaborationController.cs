using IT15_MonticalboFinalProj.Models;
using IT15_MonticalboFinalProj.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IT15_MonticalboFinalProj.Controllers;

[Authorize]
public class CollaborationController : Controller
{
    private readonly IProjectMessageRepository _messageRepo;
    private readonly INotificationRepository _notificationRepo;
    private readonly IProjectRepository _projectRepo;
    private readonly UserManager<ApplicationUser> _userManager;

    public CollaborationController(
        IProjectMessageRepository messageRepo,
        INotificationRepository notificationRepo,
        IProjectRepository projectRepo,
        UserManager<ApplicationUser> userManager)
    {
        _messageRepo = messageRepo;
        _notificationRepo = notificationRepo;
        _projectRepo = projectRepo;
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

    [HttpGet]
    public async Task<IActionResult> GetMessages(int projectId)
    {
        var me = await _userManager.GetUserAsync(User);
        if (me == null || !await IsAuthorizedForProject(projectId, me)) return Unauthorized();

        var messages = await _messageRepo.GetByProjectAsync(projectId);
        return Json(messages.Select(m => new {
            m.Sender?.FullName,
            m.Content,
            Timestamp = m.Timestamp.ToString("g")
        }));
    }

    [HttpPost]
    public async Task<IActionResult> SendMessage(int projectId, string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return Json(new { success = false });

        var me = await _userManager.GetUserAsync(User);
        if (me == null || !await IsAuthorizedForProject(projectId, me)) return Json(new { success = false, message = "Unauthorized" });

        var msg = new ProjectMessage
        {
            ProjectId = projectId,
            SenderId = me.Id,
            Content = content,
            Timestamp = DateTime.UtcNow
        };

        await _messageRepo.AddAsync(msg);

        // Create notifications for other members (optional logic here)
        
        return Json(new { success = true });
    }

    [HttpGet]
    public async Task<IActionResult> Notifications()
    {
        var me = await _userManager.GetUserAsync(User);
        if (me == null) return Unauthorized();
        
        var notifications = await _notificationRepo.GetByUserAsync(me.Id);
        return View(notifications);
    }

    [HttpPost]
    public async Task<IActionResult> MarkRead(int id)
    {
        await _notificationRepo.MarkAsReadAsync(id);
        return Json(new { success = true });
    }
}
