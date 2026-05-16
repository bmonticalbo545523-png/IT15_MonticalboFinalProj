using IT15_MonticalboFinalProj.Models;

namespace IT15_MonticalboFinalProj.Repositories;

public interface IProjectTaskRepository
{
    Task<List<ProjectTask>> GetByProjectAsync(int projectId);
    Task<List<ProjectTask>> GetArchivedByProjectAsync(int projectId);
    Task<List<ProjectTask>> GetArchivedByAdviserAsync(string adviserId);
    Task<List<ProjectTask>> GetAllAsync(int? departmentId = null);
    Task<ProjectTask?> GetByIdAsync(int id);
    Task<bool> AddAsync(ProjectTask task);
    Task<bool> UpdateAsync(ProjectTask task);
    Task<bool> ArchiveAsync(int id);
    Task<bool> RestoreAsync(int id);
    Task<int> CountByProjectAsync(int projectId);
    Task<int> CountByUserAsync(string userId);
    Task<int> CountByStatusAsync(string status, int? departmentId = null);
}
