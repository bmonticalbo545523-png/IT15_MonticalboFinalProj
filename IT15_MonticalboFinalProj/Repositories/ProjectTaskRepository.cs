using IT15_MonticalboFinalProj.Data;
using IT15_MonticalboFinalProj.Models;
using Microsoft.EntityFrameworkCore;

namespace IT15_MonticalboFinalProj.Repositories;

public class ProjectTaskRepository : IProjectTaskRepository
{
    private readonly ApplicationDbContext _context;

    public ProjectTaskRepository(ApplicationDbContext context)
        => _context = context;

    public async Task<List<ProjectTask>> GetByProjectAsync(int projectId)
        => await _context.ProjectTasks
            .Include(t => t.AssignedTo)
            .Include(t => t.CreatedBy)
            .Where(t => t.ProjectId == projectId && !t.IsArchived)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

    public async Task<List<ProjectTask>> GetArchivedByProjectAsync(int projectId)
        => await _context.ProjectTasks
            .Include(t => t.AssignedTo)
            .Where(t => t.ProjectId == projectId && t.IsArchived)
            .OrderByDescending(t => t.ArchivedAt)
            .ToListAsync();

    public async Task<List<ProjectTask>> GetArchivedByAdviserAsync(string adviserId)
        => await _context.ProjectTasks
            .Include(t => t.Project)
            .Include(t => t.AssignedTo)
            .Where(t => t.IsArchived && t.Project!.AdviserId == adviserId)
            .OrderByDescending(t => t.ArchivedAt)
            .ToListAsync();

    public async Task<List<ProjectTask>> GetAllAsync(int? departmentId = null)
        => await _context.ProjectTasks
            .Include(t => t.Project)
            .Include(t => t.AssignedTo)
            .Where(t => !t.IsArchived && (departmentId == null || t.Project!.DepartmentId == departmentId))
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

    public async Task<ProjectTask?> GetByIdAsync(int id)
        => await _context.ProjectTasks
            .Include(t => t.AssignedTo)
            .Include(t => t.Project)
            .FirstOrDefaultAsync(t => t.Id == id);

    public async Task<bool> AddAsync(ProjectTask task)
    {
        await _context.ProjectTasks.AddAsync(task);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> UpdateAsync(ProjectTask task)
    {
        _context.ProjectTasks.Update(task);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> ArchiveAsync(int id)
    {
        var task = await _context.ProjectTasks.FindAsync(id);
        if (task == null) return false;
        task.IsArchived = true;
        task.ArchivedAt = DateTime.UtcNow;
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> RestoreAsync(int id)
    {
        var task = await _context.ProjectTasks.FindAsync(id);
        if (task == null) return false;
        task.IsArchived = false;
        task.ArchivedAt = null;
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<int> CountByProjectAsync(int projectId)
        => await _context.ProjectTasks.CountAsync(t => t.ProjectId == projectId && !t.IsArchived);

    public async Task<int> CountByUserAsync(string userId)
        => await _context.ProjectTasks.CountAsync(t => t.AssignedToId == userId && !t.IsArchived);

    public async Task<int> CountByStatusAsync(string status, int? departmentId = null)
        => await _context.ProjectTasks.CountAsync(t => t.Status == status && !t.IsArchived && (departmentId == null || t.Project!.DepartmentId == departmentId));
}
