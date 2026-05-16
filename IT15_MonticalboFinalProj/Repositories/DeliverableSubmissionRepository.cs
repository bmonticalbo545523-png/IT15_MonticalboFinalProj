using IT15_MonticalboFinalProj.Data;
using IT15_MonticalboFinalProj.Models;
using Microsoft.EntityFrameworkCore;

namespace IT15_MonticalboFinalProj.Repositories;

public class DeliverableSubmissionRepository : IDeliverableSubmissionRepository
{
    private readonly ApplicationDbContext _context;

    public DeliverableSubmissionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<DeliverableSubmission>> GetByTaskAsync(int taskId)
    {
        return await _context.DeliverableSubmissions
            .Include(ds => ds.Student)
            .Where(ds => ds.ProjectTaskId == taskId)
            .OrderByDescending(ds => ds.SubmittedAt)
            .ToListAsync();
    }
    
    public async Task<List<DeliverableSubmission>> GetByProjectAsync(int projectId)
    {
        return await _context.DeliverableSubmissions
            .Include(ds => ds.Student)
            .Include(ds => ds.ProjectTask)
            .Where(ds => ds.ProjectTask!.ProjectId == projectId)
            .OrderByDescending(ds => ds.SubmittedAt)
            .ToListAsync();
    }

    public async Task<DeliverableSubmission?> GetByIdAsync(int id)
    {
        return await _context.DeliverableSubmissions.FindAsync(id);
    }

    public async Task<DeliverableSubmission?> GetLatestByStudentAndTaskAsync(string studentId, int taskId)
    {
        return await _context.DeliverableSubmissions
            .Where(ds => ds.StudentId == studentId && ds.ProjectTaskId == taskId)
            .OrderByDescending(ds => ds.SubmittedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> AddAsync(DeliverableSubmission submission)
    {
        _context.DeliverableSubmissions.Add(submission);
        return await _context.SaveChangesAsync() > 0;
    }
}
