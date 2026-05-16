using IT15_MonticalboFinalProj.Models;

namespace IT15_MonticalboFinalProj.Repositories;

public interface IDepartmentRepository
{
    Task<List<Department>> GetAllAsync();
    Task<List<Department>> GetArchivedAsync();
    Task<Department?> GetByIdAsync(int id);
    Task<bool> AddAsync(Department department);
    Task<bool> UpdateAsync(Department department);
    Task<bool> InactivateAsync(int id);
    Task<bool> RestoreAsync(int id);
    Task<bool> DeleteAsync(int id);
}
