using IT15_MonticalboFinalProj.Data;
using IT15_MonticalboFinalProj.Models;
using Microsoft.EntityFrameworkCore;

namespace IT15_MonticalboFinalProj.Repositories;

public class AnnouncementRepository : IAnnouncementRepository
{
    private readonly ApplicationDbContext _context;

    public AnnouncementRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Announcement>> GetByProjectAsync(int projectId)
    {
        return await _context.Announcements
            .Include(a => a.Author)
            .Where(a => a.ProjectId == projectId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Announcement>> GetAllForStudentAsync(string studentId)
    {
        var projectIds = await _context.ProjectMembers
            .Where(pm => pm.StudentId == studentId)
            .Select(pm => pm.ProjectId)
            .ToListAsync();

        return await _context.Announcements
            .Include(a => a.Author)
            .Include(a => a.Project)
            .Where(a => projectIds.Contains(a.ProjectId))
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<Announcement?> GetByIdAsync(int id)
    {
        return await _context.Announcements.FindAsync(id);
    }

    public async Task<bool> AddAsync(Announcement announcement)
    {
        _context.Announcements.Add(announcement);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var a = await _context.Announcements.FindAsync(id);
        if (a == null) return false;
        _context.Announcements.Remove(a);
        return await _context.SaveChangesAsync() > 0;
    }
}
