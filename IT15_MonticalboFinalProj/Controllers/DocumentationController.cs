using IT15_MonticalboFinalProj.Models;
using IT15_MonticalboFinalProj.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IT15_MonticalboFinalProj.Controllers;

[Authorize]
public class DocumentationController : Controller
{
    private readonly IResearchDocumentRepository _documentRepo;
    private readonly IProjectRepository _projectRepo;
    private readonly IDeliverableSubmissionRepository _submissionRepo;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWebHostEnvironment _environment;

    public DocumentationController(
        IResearchDocumentRepository documentRepo,
        IProjectRepository projectRepo,
        IDeliverableSubmissionRepository submissionRepo,
        UserManager<ApplicationUser> userManager,
        IWebHostEnvironment environment)
    {
        _documentRepo = documentRepo;
        _projectRepo = projectRepo;
        _submissionRepo = submissionRepo;
        _userManager = userManager;
        _environment = environment;
    }

    public async Task<IActionResult> Index()
    {
        var me = await _userManager.GetUserAsync(User);
        if (me == null) return RedirectToAction("Login", "Account");

        var projects = await _projectRepo.GetByStatusAsync("Completed", me.DepartmentId);
        return View(projects);
    }

    private async Task<bool> IsAuthorizedForProject(int projectId, ApplicationUser user)
    {
        var project = await _projectRepo.GetByIdAsync(projectId);
        if (project == null) return false;

        // If project is completed, everyone in the department can see it
        if (project.Status == "Completed" && project.DepartmentId == user.DepartmentId) return true;

        if (await _userManager.IsInRoleAsync(user, "SuperAdmin") || await _userManager.IsInRoleAsync(user, "Administrator")) return true;
        if (project.AdviserId == user.Id) return true;
        
        var studentProjects = await _projectRepo.GetByStudentAsync(user.Id);
        return studentProjects.Any(p => p.Id == projectId);
    }

    public async Task<IActionResult> ProjectDocuments(int projectId)
    {
        var me = await _userManager.GetUserAsync(User);
        if (me == null || !await IsAuthorizedForProject(projectId, me)) return Unauthorized();
        
        var project = await _projectRepo.GetByIdAsync(projectId);
        var docs = await _documentRepo.GetByProjectAsync(projectId);
        var submissions = await _submissionRepo.GetByProjectAsync(projectId);
        
        ViewBag.ProjectId = projectId;
        ViewBag.ProjectName = project?.ProjectName;
        ViewBag.Submissions = submissions;
        
        return View(docs);
    }

    [HttpGet]
    public async Task<IActionResult> GetProjectDocuments(int projectId)
    {
        var me = await _userManager.GetUserAsync(User);
        if (me == null || !await IsAuthorizedForProject(projectId, me)) return Unauthorized();

        var docs = await _documentRepo.GetByProjectAsync(projectId);
        return Json(docs.Select(d => new {
            d.Id,
            d.Title,
            UploadedBy = d.UploadedBy?.FullName ?? "Unknown",
            UploadedAt = d.UploadedAt.ToString("MMM dd, yyyy"),
            d.FilePath,
            d.IsFinal
        }));
    }

    [HttpPost]
    public async Task<IActionResult> Upload(int projectId, string title, string @abstract, IFormFile file, bool isFinal)
    {
        if (file == null || file.Length == 0) return Json(new { success = false, message = "No file selected" });

        var me = await _userManager.GetUserAsync(User);
        if (me == null || !await IsAuthorizedForProject(projectId, me)) return Json(new { success = false, message = "Unauthorized" });

        var project = await _projectRepo.GetByIdAsync(projectId);
        if (project == null) return Json(new { success = false, message = "Project not found" });

        // Save file
        string webRootPath = _environment.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRootPath))
        {
            webRootPath = Path.Combine(_environment.ContentRootPath, "wwwroot");
        }
        
        string uploadsFolder = Path.Combine(webRootPath, "uploads", "research_docs");
        if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

        string uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
        string filePath = Path.Combine(uploadsFolder, uniqueFileName);

        using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(fileStream);
        }

        var doc = new ResearchDocument
        {
            ProjectId = projectId,
            Title = title,
            Abstract = @abstract,
            FilePath = "/uploads/research_docs/" + uniqueFileName,
            UploadedById = me?.Id,
            UploadedAt = DateTime.UtcNow,
            IsFinal = isFinal
        };

        await _documentRepo.AddAsync(doc);

        return Json(new { success = true, message = "Document uploaded successfully" });
    }
}
