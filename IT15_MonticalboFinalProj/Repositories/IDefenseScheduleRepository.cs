using IT15_MonticalboFinalProj.Models;

namespace IT15_MonticalboFinalProj.Repositories;

public interface IDefenseScheduleRepository
{
    Task<List<DefenseSchedule>> GetAllAsync(int? departmentId = null);
    Task<List<DefenseSchedule>> GetByProjectAsync(int projectId);
    Task<List<DefenseSchedule>> GetByAdviserAsync(string adviserId);
    Task<List<DefenseSchedule>> GetByStudentAsync(string studentId);
    Task<DefenseSchedule?> GetByIdAsync(int id);
    Task<bool> AddAsync(DefenseSchedule schedule);
    Task<bool> UpdateAsync(DefenseSchedule schedule);

    // Adviser Availability
    Task<List<AdviserAvailability>> GetAvailabilityByAdviserAsync(string adviserId);
    Task<List<AdviserAvailability>> GetAvailableSlotsByAdviserAsync(string adviserId);
    Task<AdviserAvailability?> GetAvailabilityByIdAsync(int id);
    Task<bool> AddAvailabilityAsync(AdviserAvailability availability);
    Task<bool> RemoveAvailabilityAsync(int id);
    Task<bool> BookSlotAsync(int id);
}
