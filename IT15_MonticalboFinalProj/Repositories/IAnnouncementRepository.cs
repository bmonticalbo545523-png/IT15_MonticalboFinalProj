using IT15_MonticalboFinalProj.Models;

namespace IT15_MonticalboFinalProj.Repositories;

public interface IAnnouncementRepository
{
    Task<List<Announcement>> GetByProjectAsync(int projectId);
    Task<List<Announcement>> GetAllForStudentAsync(string studentId);
    Task<Announcement?> GetByIdAsync(int id);
    Task<bool> AddAsync(Announcement announcement);
    Task<bool> DeleteAsync(int id);
}
