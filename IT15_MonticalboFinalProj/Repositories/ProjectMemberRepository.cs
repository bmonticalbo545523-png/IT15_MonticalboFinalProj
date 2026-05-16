using IT15_MonticalboFinalProj.Data;
using IT15_MonticalboFinalProj.Models;
using Microsoft.EntityFrameworkCore;

namespace IT15_MonticalboFinalProj.Repositories;

public class ProjectMemberRepository : IProjectMemberRepository
{
    private readonly ApplicationDbContext _context;

    public ProjectMemberRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ProjectMember>> GetByProjectAsync(int projectId)
    {
        return await _context.ProjectMembers
            .Include(pm => pm.Student)
            .Where(pm => pm.ProjectId == projectId)
            .ToListAsync();
    }

    public async Task<List<ProjectMember>> GetByStudentAsync(string studentId)
    {
        return await _context.ProjectMembers
            .Include(pm => pm.Project)
            .Where(pm => pm.StudentId == studentId)
            .ToListAsync();
    }

    public async Task<bool> AddAsync(ProjectMember member)
    {
        _context.ProjectMembers.Add(member);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> RemoveAsync(int projectId, string studentId)
    {
        var member = await _context.ProjectMembers
            .FirstOrDefaultAsync(pm => pm.ProjectId == projectId && pm.StudentId == studentId);
        if (member == null) return false;
        _context.ProjectMembers.Remove(member);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> IsStudentInProjectAsync(int projectId, string studentId)
    {
        return await _context.ProjectMembers
            .AnyAsync(pm => pm.ProjectId == projectId && pm.StudentId == studentId);
    }
    public async Task<int> GetCountByProjectAsync(int projectId)
    {
        return await _context.ProjectMembers.CountAsync(pm => pm.ProjectId == projectId);
    }
}
