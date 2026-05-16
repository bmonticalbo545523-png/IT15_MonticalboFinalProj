using IT15_MonticalboFinalProj.Models;

namespace IT15_MonticalboFinalProj.Repositories;

public interface IAuditLogRepository
{
    Task<List<AuditLog>> GetAllAsync(int take = 100);
    Task<bool> AddAsync(AuditLog log);
    Task<int> CountAsync();
}
