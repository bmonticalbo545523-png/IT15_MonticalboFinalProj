using IT15_MonticalboFinalProj.Models;

namespace IT15_MonticalboFinalProj.Repositories;

public interface IProjectMemberRepository
{
    Task<List<ProjectMember>> GetByProjectAsync(int projectId);
    Task<List<ProjectMember>> GetByStudentAsync(string studentId);
    Task<bool> AddAsync(ProjectMember member);
    Task<bool> RemoveAsync(int projectId, string studentId);
    Task<bool> IsStudentInProjectAsync(int projectId, string studentId);
    Task<int> GetCountByProjectAsync(int projectId);
}
