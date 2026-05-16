using IT15_MonticalboFinalProj.Data;
using IT15_MonticalboFinalProj.Models;
using Microsoft.EntityFrameworkCore;

namespace IT15_MonticalboFinalProj.Repositories;

public class ProjectMilestoneRepository : IProjectMilestoneRepository
{
    private readonly ApplicationDbContext _context;
    public ProjectMilestoneRepository(ApplicationDbContext context) => _context = context;

    public async Task<List<ProjectMilestone>> GetByProjectAsync(int projectId)
        => await _context.ProjectMilestones
            .Where(m => m.ProjectId == projectId)
            .OrderBy(m => m.TargetDate)
            .ToListAsync();

    public async Task<ProjectMilestone?> GetByIdAsync(int id)
        => await _context.ProjectMilestones.FindAsync(id);

    public async Task<bool> AddAsync(ProjectMilestone milestone)
    {
        await _context.ProjectMilestones.AddAsync(milestone);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> UpdateAsync(ProjectMilestone milestone)
    {
        _context.ProjectMilestones.Update(milestone);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var m = await _context.ProjectMilestones.FindAsync(id);
        if (m == null) return false;
        _context.ProjectMilestones.Remove(m);
        return await _context.SaveChangesAsync() > 0;
    }
}

public class ProjectStageRepository : IProjectStageRepository
{
    private readonly ApplicationDbContext _context;
    public ProjectStageRepository(ApplicationDbContext context) => _context = context;

    public async Task<List<ProjectStageLog>> GetByProjectAsync(int projectId)
        => await _context.ProjectStageLogs
            .Where(l => l.ProjectId == projectId)
            .OrderByDescending(l => l.StartedAt)
            .ToListAsync();

    public async Task<ProjectStageLog?> GetCurrentStageAsync(int projectId)
        => await _context.ProjectStageLogs
            .Where(l => l.ProjectId == projectId && l.Status == "Current")
            .FirstOrDefaultAsync();

    public async Task<bool> AddLogAsync(ProjectStageLog log)
    {
        await _context.ProjectStageLogs.AddAsync(log);
        return await _context.SaveChangesAsync() > 0;
    }
}
