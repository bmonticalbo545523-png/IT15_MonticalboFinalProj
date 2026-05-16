using IT15_MonticalboFinalProj.Data;
using IT15_MonticalboFinalProj.Models;
using Microsoft.EntityFrameworkCore;

namespace IT15_MonticalboFinalProj.Repositories;

public class AuditLogRepository : IAuditLogRepository
{
    private readonly ApplicationDbContext _context;

    public AuditLogRepository(ApplicationDbContext context)
        => _context = context;

    public async Task<List<AuditLog>> GetAllAsync(int take = 100)
        => await _context.AuditLogs
            .Include(a => a.User)
            .OrderByDescending(a => a.Timestamp)
            .Take(take)
            .ToListAsync();

    public async Task<bool> AddAsync(AuditLog log)
    {
        await _context.AuditLogs.AddAsync(log);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<int> CountAsync()
        => await _context.AuditLogs.CountAsync();
}
