using IT15_MonticalboFinalProj.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace IT15_MonticalboFinalProj.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var context = services.GetRequiredService<ApplicationDbContext>();

        await context.Database.MigrateAsync();

        // ── Seed Departments ────────────────────────────────────
        if (!await context.Departments.AnyAsync())
        {
            context.Departments.AddRange(
                new Department { Name = "General Administration", Description = "Default administrative department" },
                new Department { Name = "Computer Science", Description = "Academic department for CS" },
                new Department { Name = "Information Technology", Description = "Academic department for IT" }
            );
            await context.SaveChangesAsync();
        }
        var defaultDept = await context.Departments.FirstAsync();

        // ── Seed Roles ──────────────────────────────────────────
        string[] roles = { "SuperAdmin", "Administrator", "ResearchAdviser", "Student" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        // ── Seed Admin User ─────────────────────────────────────
        if (await userManager.FindByNameAsync("admin") == null)
        {
            var admin = new ApplicationUser
            {
                UserName = "admin",
                Email = "admin@projexis.com",
                FullName = "System Administrator",
                EmailConfirmed = true,
                Status = "Active",
                CreatedAt = DateTime.UtcNow,
                IsProfileComplete = true,
                DepartmentId = defaultDept.Id,
                IsActive = true
            };
            var result = await userManager.CreateAsync(admin, "Admin123!");
            if (result.Succeeded)
                await userManager.AddToRoleAsync(admin, "SuperAdmin");
        }

        // ── Seed Student ────────────────────────────────────────
        if (await userManager.FindByNameAsync("student") == null)
        {
            var student = new ApplicationUser
            {
                UserName = "student",
                Email = "student@projexis.com",
                FullName = "Sample Student",
                EmailConfirmed = true,
                Status = "Active",
                CreatedAt = DateTime.UtcNow,
                IsProfileComplete = true,
                StudentIdNumber = "2024-DEMO-001",
                ProgramOrCourse = "BS Computer Science",
                DepartmentId = defaultDept.Id,
                IsActive = true
            };
            var result = await userManager.CreateAsync(student, "Admin123!");
            if (result.Succeeded)
                await userManager.AddToRoleAsync(student, "Student");
        }

        // ── Seed Default System Setting ─────────────────────────
        if (!await context.SystemSettings.AnyAsync())
        {
            context.SystemSettings.Add(new SystemSetting
            {
                SystemName = "Projexis",
                Theme = "light"
            });
            await context.SaveChangesAsync();
        }

        // ── Seed Sample Research Group ──────────────────────────
        if (!await context.Projects.AnyAsync())
        {
            var admin = await userManager.FindByNameAsync("admin");
            var student = await userManager.FindByNameAsync("student");

            if (admin != null && student != null)
            {
                var group = new Project
                {
                    ProjectName = "AI in Academic Research",
                    Description = "Exploring the impact of AI tools on student learning outcomes.",
                    StartDate = DateTime.Today,
                    Status = "In Progress",
                    CreatedById = admin.Id,
                    AdviserId = admin.Id, // Admin acting as adviser for seed data
                    IsAcceptedByAdviser = true,
                    DepartmentId = admin.DepartmentId
                };
                context.Projects.Add(group);
                await context.SaveChangesAsync();

                // Add student to group
                context.ProjectMembers.Add(new ProjectMember { ProjectId = group.Id, StudentId = student.Id });
                
                // Add sample deliverable
                context.ProjectTasks.Add(new ProjectTask 
                { 
                    ProjectId = group.Id, 
                    Title = "Research Proposal", 
                    Description = "Submit the initial research proposal document.",
                    Status = "Pending",
                    Priority = "High",
                    Type = "Document",
                    DueDate = DateTime.Today.AddDays(7)
                });

                await context.SaveChangesAsync();
            }
        }
        // ── Seed Dummy Research Documents for all Departments ───
        var departments = await context.Departments.ToListAsync();
        var adminUser = await userManager.FindByNameAsync("admin") ?? await userManager.Users.FirstOrDefaultAsync();
        
        foreach (var dept in departments)
        {
            // Ensure each department has a sample project to hold documents
            var deptProject = await context.Projects.FirstOrDefaultAsync(p => p.DepartmentId == dept.Id);
            if (deptProject == null && adminUser != null)
            {
                deptProject = new Project
                {
                    ProjectName = $"Sample {dept.Name} Research",
                    Description = $"A default research project for {dept.Name} department.",
                    Status = "Completed",
                    CurrentStage = "Implementation",
                    ProgressPercentage = 100,
                    DepartmentId = dept.Id,
                    CreatedById = adminUser.Id,
                    CreatedAt = DateTime.UtcNow
                };
                context.Projects.Add(deptProject);
                await context.SaveChangesAsync();
            }

            // Check if this specific project/department has any documents yet
            if (deptProject != null && adminUser != null && !await context.ResearchDocuments.AnyAsync(d => d.ProjectId == deptProject.Id))
            {
                context.ResearchDocuments.AddRange(
                    new ResearchDocument 
                    {
                        ProjectId = deptProject.Id,
                        Title = $"[Archive] Advances in {dept.Name} Technology",
                        Abstract = $"This foundational paper explores historical and modern breakthroughs specifically within the {dept.Name} domain.",
                        FilePath = "/images/logo.png",
                        UploadedById = adminUser.Id,
                        UploadedAt = DateTime.UtcNow.AddMonths(-2),
                        IsFinal = true,
                        Version = "1.0"
                    },
                    new ResearchDocument 
                    {
                        ProjectId = deptProject.Id,
                        Title = $"{dept.Name} Student Thesis 2024",
                        Abstract = "A comprehensive study conducted by senior students regarding the current industry trends and challenges.",
                        FilePath = "/images/logo.png",
                        UploadedById = adminUser.Id,
                        UploadedAt = DateTime.UtcNow.AddMonths(-1),
                        IsFinal = true,
                        Version = "Final"
                    }
                );
                await context.SaveChangesAsync();
            }
        }

        // ── Patch Existing Projects with DepartmentId ──────────
        try
        {
            var projectsToPatch = await context.Projects
                .Include(p => p.CreatedBy)
                .Where(p => p.DepartmentId == null && p.CreatedById != null)
                .ToListAsync();
            foreach (var p in projectsToPatch)
            {
                p.DepartmentId = p.CreatedBy?.DepartmentId;
            }
            if (projectsToPatch.Any()) await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Patch failed: " + ex.Message);
        }
    }
}
