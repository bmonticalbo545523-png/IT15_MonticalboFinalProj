using IT15_MonticalboFinalProj.Data;
using IT15_MonticalboFinalProj.Models;
using Microsoft.EntityFrameworkCore;

namespace IT15_MonticalboFinalProj.Repositories;

public class ResearchDocumentRepository : IResearchDocumentRepository
{
    private readonly ApplicationDbContext _context;
    public ResearchDocumentRepository(ApplicationDbContext context) => _context = context;

    public async Task<List<ResearchDocument>> GetByProjectAsync(int projectId)
        => await _context.ResearchDocuments
            .Include(d => d.UploadedBy)
            .Where(d => d.ProjectId == projectId)
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync();

    public async Task<List<ResearchDocument>> GetByDepartmentAsync(int departmentId)
        => await _context.ResearchDocuments
            .Include(d => d.Project)
            .Include(d => d.UploadedBy)
            .Where(d => d.Project!.DepartmentId == departmentId && (d.IsFinal || d.Project.Status == "Completed"))
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync();

    public async Task<ResearchDocument?> GetByIdAsync(int id)
        => await _context.ResearchDocuments.Include(d => d.Project).FirstOrDefaultAsync(d => d.Id == id);

    public async Task<bool> AddAsync(ResearchDocument document)
    {
        await _context.ResearchDocuments.AddAsync(document);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> UpdateAsync(ResearchDocument document)
    {
        _context.ResearchDocuments.Update(document);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var d = await _context.ResearchDocuments.FindAsync(id);
        if (d == null) return false;
        _context.ResearchDocuments.Remove(d);
        return await _context.SaveChangesAsync() > 0;
    }
}

public class ProjectMessageRepository : IProjectMessageRepository
{
    private readonly ApplicationDbContext _context;
    public ProjectMessageRepository(ApplicationDbContext context) => _context = context;

    public async Task<List<ProjectMessage>> GetByProjectAsync(int projectId, int count = 50)
        => await _context.ProjectMessages
            .Include(m => m.Sender)
            .Where(m => m.ProjectId == projectId)
            .OrderByDescending(m => m.Timestamp)
            .Take(count)
            .OrderBy(m => m.Timestamp)
            .ToListAsync();

    public async Task<bool> AddAsync(ProjectMessage message)
    {
        await _context.ProjectMessages.AddAsync(message);
        return await _context.SaveChangesAsync() > 0;
    }
}

public class NotificationRepository : INotificationRepository
{
    private readonly ApplicationDbContext _context;
    public NotificationRepository(ApplicationDbContext context) => _context = context;

    public async Task<List<Notification>> GetByUserAsync(string userId)
        => await _context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(20)
            .ToListAsync();

    public async Task<int> GetUnreadCountAsync(string userId)
        => await _context.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);

    public async Task<bool> AddAsync(Notification notification)
    {
        await _context.Notifications.AddAsync(notification);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> MarkAsReadAsync(int id)
    {
        var n = await _context.Notifications.FindAsync(id);
        if (n == null) return false;
        n.IsRead = true;
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> MarkAllAsReadAsync(string userId)
    {
        var unread = await _context.Notifications.Where(n => n.UserId == userId && !n.IsRead).ToListAsync();
        foreach (var n in unread) n.IsRead = true;
        return await _context.SaveChangesAsync() > 0;
    }
}
