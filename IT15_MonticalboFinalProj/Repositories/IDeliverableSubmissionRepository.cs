using IT15_MonticalboFinalProj.Models;

namespace IT15_MonticalboFinalProj.Repositories;

public interface IDeliverableSubmissionRepository
{
    Task<List<DeliverableSubmission>> GetByTaskAsync(int taskId);
    Task<List<DeliverableSubmission>> GetByProjectAsync(int projectId);
    Task<DeliverableSubmission?> GetByIdAsync(int id);
    Task<DeliverableSubmission?> GetLatestByStudentAndTaskAsync(string studentId, int taskId);
    Task<bool> AddAsync(DeliverableSubmission submission);
}
