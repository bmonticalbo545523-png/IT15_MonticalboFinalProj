using IT15_MonticalboFinalProj.Data;
using IT15_MonticalboFinalProj.Models;
using Microsoft.EntityFrameworkCore;

namespace IT15_MonticalboFinalProj.Repositories;

public class DepartmentRepository : IDepartmentRepository
{
    private readonly ApplicationDbContext _context;

    public DepartmentRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Department>> GetAllAsync()
        => await _context.Departments
            .Where(d => d.IsActive)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();

    public async Task<List<Department>> GetArchivedAsync()
        => await _context.Departments
            .Where(d => !d.IsActive)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();

    public async Task<Department?> GetByIdAsync(int id)
        => await _context.Departments.FindAsync(id);

    public async Task<bool> AddAsync(Department department)
    {
        _context.Departments.Add(department);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> UpdateAsync(Department department)
    {
        _context.Departments.Update(department);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> InactivateAsync(int id)
    {
        var department = await _context.Departments.FindAsync(id);
        if (department == null) return false;

        department.IsActive = false;

        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> RestoreAsync(int id)
    {
        var department = await _context.Departments.FindAsync(id);
        if (department == null) return false;

        department.IsActive = true;
        // Restoring a department activates only the department without automatically restoring its users.
        
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var department = await _context.Departments.FindAsync(id);
        if (department == null) return false;
        _context.Departments.Remove(department);
        return await _context.SaveChangesAsync() > 0;
    }
}
