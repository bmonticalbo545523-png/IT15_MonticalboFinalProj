using IT15_MonticalboFinalProj.Models;

namespace IT15_MonticalboFinalProj.Repositories;

public interface IProjectMilestoneRepository
{
    Task<List<ProjectMilestone>> GetByProjectAsync(int projectId);
    Task<ProjectMilestone?> GetByIdAsync(int id);
    Task<bool> AddAsync(ProjectMilestone milestone);
    Task<bool> UpdateAsync(ProjectMilestone milestone);
    Task<bool> DeleteAsync(int id);
}

public interface IProjectStageRepository
{
    Task<List<ProjectStageLog>> GetByProjectAsync(int projectId);
    Task<ProjectStageLog?> GetCurrentStageAsync(int projectId);
    Task<bool> AddLogAsync(ProjectStageLog log);
}
