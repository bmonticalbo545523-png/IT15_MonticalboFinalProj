using IT15_MonticalboFinalProj.Data;
using IT15_MonticalboFinalProj.Models;
using Microsoft.EntityFrameworkCore;

namespace IT15_MonticalboFinalProj.Repositories;

public class DeliverableCommentRepository : IDeliverableCommentRepository
{
    private readonly ApplicationDbContext _context;

    public DeliverableCommentRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<DeliverableComment>> GetByTaskAsync(int taskId)
    {
        return await _context.DeliverableComments
            .Include(dc => dc.Author)
            .Where(dc => dc.ProjectTaskId == taskId)
            .OrderBy(dc => dc.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> AddAsync(DeliverableComment comment)
    {
        _context.DeliverableComments.Add(comment);
        return await _context.SaveChangesAsync() > 0;
    }
}
