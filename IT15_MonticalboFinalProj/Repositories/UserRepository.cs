using IT15_MonticalboFinalProj.Data;
using IT15_MonticalboFinalProj.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace IT15_MonticalboFinalProj.Repositories;

public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public UserRepository(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<List<ApplicationUser>> GetAllAsync(int? departmentId = null)
        => await _context.Users
            .Include(u => u.Department)
            .Where(u => u.IsActive && (departmentId == null || u.DepartmentId == departmentId))
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync();

    public async Task<List<ApplicationUser>> GetArchivedAsync(int? departmentId = null)
        => await _context.Users
            .Include(u => u.Department)
            .Where(u => !u.IsActive && (departmentId == null || u.DepartmentId == departmentId))
            .OrderByDescending(u => u.ArchivedAt)
            .ToListAsync();

    public async Task<List<ApplicationUser>> GetByRoleAsync(string role, int? departmentId = null)
    {
        var users = await _userManager.GetUsersInRoleAsync(role);
        return users.Where(u => u.IsActive && (departmentId == null || u.DepartmentId == departmentId))
                    .OrderByDescending(u => u.CreatedAt)
                    .ToList();
    }

    public async Task<List<ApplicationUser>> GetByRoleArchivedAsync(string role, int? departmentId = null)
    {
        var users = await _userManager.GetUsersInRoleAsync(role);
        return users.Where(u => !u.IsActive && (departmentId == null || u.DepartmentId == departmentId))
                    .OrderByDescending(u => u.ArchivedAt)
                    .ToList();
    }

    public async Task<ApplicationUser?> GetByIdAsync(string id)
        => await _context.Users.FindAsync(id);

    public async Task<bool> UpdateAsync(ApplicationUser user)
    {
        _context.Users.Update(user);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> ArchiveAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return false;

        // Prevent deactivating SuperAdmin
        if (await _userManager.IsInRoleAsync(user, "SuperAdmin"))
            return false;

        user.IsActive = false;
        user.ArchivedAt = DateTime.UtcNow;
        user.Status = "Inactive";

        // Invalidate current sessions
        await _userManager.UpdateSecurityStampAsync(user);
        
        return (await _userManager.UpdateAsync(user)).Succeeded;
    }

    public async Task<bool> RestoreAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return false;

        user.IsActive = true;
        user.ArchivedAt = null;
        user.Status = "Active";

        return (await _userManager.UpdateAsync(user)).Succeeded;
    }

    public async Task<int> CountAsync(int? departmentId = null)
        => await _context.Users.CountAsync(u => u.IsActive && (departmentId == null || u.DepartmentId == departmentId));

    public async Task<int> CountByRoleAsync(string role, int? departmentId = null)
    {
        var users = await _userManager.GetUsersInRoleAsync(role);
        return users.Count(u => u.IsActive && (departmentId == null || u.DepartmentId == departmentId));
    }
}
