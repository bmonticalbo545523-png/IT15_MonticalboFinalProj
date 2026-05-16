using IT15_MonticalboFinalProj.Models;

namespace IT15_MonticalboFinalProj.Repositories;

public interface IResearchDocumentRepository
{
    Task<List<ResearchDocument>> GetByProjectAsync(int projectId);
    Task<List<ResearchDocument>> GetByDepartmentAsync(int departmentId);
    Task<ResearchDocument?> GetByIdAsync(int id);
    Task<bool> AddAsync(ResearchDocument document);
    Task<bool> UpdateAsync(ResearchDocument document);
    Task<bool> DeleteAsync(int id);
}

public interface IProjectMessageRepository
{
    Task<List<ProjectMessage>> GetByProjectAsync(int projectId, int count = 50);
    Task<bool> AddAsync(ProjectMessage message);
}

public interface INotificationRepository
{
    Task<List<Notification>> GetByUserAsync(string userId);
    Task<int> GetUnreadCountAsync(string userId);
    Task<bool> AddAsync(Notification notification);
    Task<bool> MarkAsReadAsync(int id);
    Task<bool> MarkAllAsReadAsync(string userId);
}
