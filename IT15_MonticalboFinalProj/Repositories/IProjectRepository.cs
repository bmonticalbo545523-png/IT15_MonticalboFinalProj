using IT15_MonticalboFinalProj.Models;

namespace IT15_MonticalboFinalProj.Repositories;

public interface IProjectRepository
{
    Task<List<Project>> GetAllAsync(int? departmentId = null);
    Task<List<Project>> GetArchivedAsync(int? departmentId = null);
    Task<List<Project>> GetByUserAsync(string userId);
    Task<Project?> GetByIdAsync(int id);
    Task<List<Project>> GetByAdviserAsync(string adviserId);
    Task<List<Project>> GetByStudentAsync(string studentId);
    Task<bool> AddAsync(Project project);
    Task<bool> UpdateAsync(Project project);
    Task<bool> ArchiveAsync(int id);
    Task<bool> RestoreAsync(int id);
    Task<int> CountAsync(int? departmentId = null);
    Task<int> CountByStatusAsync(string status, int? departmentId = null);
    Task<List<Project>> GetByStatusAsync(string status, int? departmentId = null);
    Task<int> CountByUserAsync(string userId);
}
