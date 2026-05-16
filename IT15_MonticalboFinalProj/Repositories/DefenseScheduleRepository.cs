using IT15_MonticalboFinalProj.Data;
using IT15_MonticalboFinalProj.Models;
using Microsoft.EntityFrameworkCore;

namespace IT15_MonticalboFinalProj.Repositories;

public class DefenseScheduleRepository : IDefenseScheduleRepository
{
    private readonly ApplicationDbContext _context;

    public DefenseScheduleRepository(ApplicationDbContext context)
        => _context = context;

    public async Task<List<DefenseSchedule>> GetAllAsync(int? departmentId = null)
        => await _context.DefenseSchedules
            .Include(ds => ds.Project).ThenInclude(p => p.Adviser)
            .Include(ds => ds.Project).ThenInclude(p => p.Members).ThenInclude(m => m.Student)
            .Include(ds => ds.ScheduledBy)
            .Include(ds => ds.ApprovedByAdviser)
            .Where(ds => departmentId == null || ds.Project!.DepartmentId == departmentId)
            .OrderByDescending(ds => ds.CreatedAt)
            .ToListAsync();

    public async Task<List<DefenseSchedule>> GetByProjectAsync(int projectId)
        => await _context.DefenseSchedules
            .Include(ds => ds.Project)
            .Include(ds => ds.ScheduledBy)
            .Where(ds => ds.ProjectId == projectId)
            .OrderByDescending(ds => ds.ScheduledDate)
            .ToListAsync();

    public async Task<List<DefenseSchedule>> GetByAdviserAsync(string adviserId)
        => await _context.DefenseSchedules
            .Include(ds => ds.Project).ThenInclude(p => p.Members).ThenInclude(m => m.Student)
            .Include(ds => ds.ScheduledBy)
            .Where(ds => ds.Project!.AdviserId == adviserId)
            .OrderByDescending(ds => ds.ScheduledDate)
            .ToListAsync();

    public async Task<List<DefenseSchedule>> GetByStudentAsync(string studentId)
    {
        var projectIds = await _context.ProjectMembers
            .Where(pm => pm.StudentId == studentId)
            .Select(pm => pm.ProjectId)
            .ToListAsync();

        return await _context.DefenseSchedules
            .Include(ds => ds.Project).ThenInclude(p => p.Adviser)
            .Include(ds => ds.ScheduledBy)
            .Where(ds => projectIds.Contains(ds.ProjectId))
            .OrderByDescending(ds => ds.ScheduledDate)
            .ToListAsync();
    }

    public async Task<DefenseSchedule?> GetByIdAsync(int id)
        => await _context.DefenseSchedules
            .Include(ds => ds.Project).ThenInclude(p => p.Adviser)
            .Include(ds => ds.Project).ThenInclude(p => p.Members).ThenInclude(m => m.Student)
            .Include(ds => ds.ScheduledBy)
            .FirstOrDefaultAsync(ds => ds.Id == id);

    public async Task<bool> AddAsync(DefenseSchedule schedule)
    {
        await _context.DefenseSchedules.AddAsync(schedule);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> UpdateAsync(DefenseSchedule schedule)
    {
        _context.DefenseSchedules.Update(schedule);
        return await _context.SaveChangesAsync() > 0;
    }

    // ─── Adviser Availability ─────────────────────────────────
    public async Task<List<AdviserAvailability>> GetAvailabilityByAdviserAsync(string adviserId)
        => await _context.AdviserAvailabilities
            .Where(a => a.AdviserId == adviserId)
            .OrderBy(a => a.AvailableDate).ThenBy(a => a.StartTime)
            .ToListAsync();

    public async Task<List<AdviserAvailability>> GetAvailableSlotsByAdviserAsync(string adviserId)
        => await _context.AdviserAvailabilities
            .Where(a => a.AdviserId == adviserId && !a.IsBooked && a.AvailableDate >= DateTime.Today)
            .OrderBy(a => a.AvailableDate).ThenBy(a => a.StartTime)
            .ToListAsync();

    public async Task<AdviserAvailability?> GetAvailabilityByIdAsync(int id)
        => await _context.AdviserAvailabilities.FindAsync(id);

    public async Task<bool> AddAvailabilityAsync(AdviserAvailability availability)
    {
        await _context.AdviserAvailabilities.AddAsync(availability);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> RemoveAvailabilityAsync(int id)
    {
        var slot = await _context.AdviserAvailabilities.FindAsync(id);
        if (slot == null) return false;
        _context.AdviserAvailabilities.Remove(slot);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> BookSlotAsync(int id)
    {
        var slot = await _context.AdviserAvailabilities.FindAsync(id);
        if (slot == null) return false;
        slot.IsBooked = true;
        return await _context.SaveChangesAsync() > 0;
    }
}
