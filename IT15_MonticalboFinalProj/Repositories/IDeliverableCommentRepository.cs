using IT15_MonticalboFinalProj.Models;

namespace IT15_MonticalboFinalProj.Repositories;

public interface IDeliverableCommentRepository
{
    Task<List<DeliverableComment>> GetByTaskAsync(int taskId);
    Task<bool> AddAsync(DeliverableComment comment);
}
