using IT15_MonticalboFinalProj.Data;
using IT15_MonticalboFinalProj.Models;
using Microsoft.EntityFrameworkCore;

namespace IT15_MonticalboFinalProj.Repositories;

public class ProjectRepository : IProjectRepository
{
    private readonly ApplicationDbContext _context;

    public ProjectRepository(ApplicationDbContext context)
        => _context = context;

    public async Task<List<Project>> GetAllAsync(int? departmentId = null)
        => await _context.Projects
            .Include(p => p.CreatedBy)
            .Include(p => p.Department)
            .Include(p => p.Adviser)
            .Include(p => p.Members)
            .Where(p => !p.IsArchived && (departmentId == null || p.DepartmentId == departmentId))
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

    public async Task<List<Project>> GetArchivedAsync(int? departmentId = null)
        => await _context.Projects
            .Include(p => p.CreatedBy)
            .Include(p => p.Department)
            .Include(p => p.Members)
            .Where(p => p.IsArchived && (departmentId == null || p.DepartmentId == departmentId))
            .OrderByDescending(p => p.ArchivedAt)
            .ToListAsync();

    public async Task<List<Project>> GetByUserAsync(string userId)
        => await _context.Projects
            .Include(p => p.CreatedBy)
            .Where(p => !p.IsArchived && p.CreatedById == userId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

    public async Task<Project?> GetByIdAsync(int id)
        => await _context.Projects
            .Include(p => p.CreatedBy)
            .Include(p => p.Adviser)
            .Include(p => p.Members).ThenInclude(m => m.Student)
            .FirstOrDefaultAsync(p => p.Id == id);

    public async Task<List<Project>> GetByAdviserAsync(string adviserId)
        => await _context.Projects
            .Include(p => p.CreatedBy)
            .Where(p => !p.IsArchived && p.AdviserId == adviserId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

    public async Task<List<Project>> GetByStudentAsync(string studentId)
    {
        var projectIds = await _context.ProjectMembers
            .Where(pm => pm.StudentId == studentId)
            .Select(pm => pm.ProjectId)
            .ToListAsync();

        return await _context.Projects
            .Include(p => p.CreatedBy)
            .Include(p => p.Adviser)
            .Where(p => !p.IsArchived && projectIds.Contains(p.Id))
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> AddAsync(Project project)
    {
        await _context.Projects.AddAsync(project);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> UpdateAsync(Project project)
    {
        _context.Projects.Update(project);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> ArchiveAsync(int id)
    {
        var project = await _context.Projects.FindAsync(id);
        if (project == null) return false;
        project.IsArchived = true;
        project.ArchivedAt = DateTime.UtcNow;
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> RestoreAsync(int id)
    {
        var project = await _context.Projects.FindAsync(id);
        if (project == null) return false;
        project.IsArchived = false;
        project.ArchivedAt = null;
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<int> CountAsync(int? departmentId = null)
        => await _context.Projects.CountAsync(p => !p.IsArchived && (departmentId == null || p.DepartmentId == departmentId));

    public async Task<int> CountByStatusAsync(string status, int? departmentId = null)
        => await _context.Projects.CountAsync(p => p.Status == status && !p.IsArchived && (departmentId == null || p.DepartmentId == departmentId));

    public async Task<List<Project>> GetByStatusAsync(string status, int? departmentId = null)
        => await _context.Projects
            .Include(p => p.Adviser)
            .Include(p => p.Members).ThenInclude(m => m.Student)
            .Include(p => p.Department)
            .Where(p => p.Status == status && !p.IsArchived && (departmentId == null || p.DepartmentId == departmentId))
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

    public async Task<int> CountByUserAsync(string userId)
        => await _context.Projects.CountAsync(p => p.CreatedById == userId && !p.IsArchived);
}
