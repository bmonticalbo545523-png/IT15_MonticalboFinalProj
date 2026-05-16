using IT15_MonticalboFinalProj.Models;

namespace IT15_MonticalboFinalProj.Repositories;

public interface IUserRepository
{
    Task<List<ApplicationUser>> GetAllAsync(int? departmentId = null);
    Task<List<ApplicationUser>> GetArchivedAsync(int? departmentId = null);
    Task<List<ApplicationUser>> GetByRoleAsync(string role, int? departmentId = null);
    Task<List<ApplicationUser>> GetByRoleArchivedAsync(string role, int? departmentId = null);
    Task<ApplicationUser?> GetByIdAsync(string id);
    Task<bool> UpdateAsync(ApplicationUser user);
    Task<bool> ArchiveAsync(string id);
    Task<bool> RestoreAsync(string id);
    Task<int> CountAsync(int? departmentId = null);
    Task<int> CountByRoleAsync(string role, int? departmentId = null);
}
