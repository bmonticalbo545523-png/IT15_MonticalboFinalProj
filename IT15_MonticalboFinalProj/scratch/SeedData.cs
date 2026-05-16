using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using IT15_MonticalboFinalProj.Data;
using IT15_MonticalboFinalProj.Models;

namespace IT15_MonticalboFinalProj.Scratch
{
    public class Seeder
    {
        public static async Task Seed(IServiceProvider serviceProvider)
        {
            using var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

            // 1. Find the Adviser (Bonie Hamilton)
            var adviser = await context.Users.FirstOrDefaultAsync(u => u.UserName == "mongies");
            if (adviser == null) return;

            // 2. Find a Student
            var student = await context.Users.FirstOrDefaultAsync(u => u.UserName == "Tibormie");
            if (student == null) return;

            // 3. Create a Dummy Project
            var project = new Project
            {
                ProjectName = "Alpha Shield Research",
                Description = "Dummy project for testing defense eligibility.",
                Status = "Proposal",
                CreatedById = adviser.Id,
                CreatedAt = DateTime.UtcNow,
                AdviserId = adviser.Id,
                DepartmentId = adviser.DepartmentId ?? 1,
                IsAcceptedByAdviser = true,
                CurrentStage = "Development",
                ProgressPercentage = 50
            };

            context.Projects.Add(project);
            await context.SaveChangesAsync();

            // 4. Add Student Member
            context.ProjectMembers.Add(new ProjectMember
            {
                ProjectId = project.Id,
                StudentId = student.Id
            });

            // 5. Add 2 Approved Deliverables
            var task1 = new ProjectTask
            {
                ProjectId = project.Id,
                Title = "Initial Documentation",
                Description = "Approved dummy deliverable",
                Status = "Approved",
                Type = "Document",
                Priority = "Medium",
                OrderIndex = 0,
                DueDate = DateTime.UtcNow.AddDays(7)
            };

            var task2 = new ProjectTask
            {
                ProjectId = project.Id,
                Title = "Architecture Design",
                Description = "Another approved dummy deliverable",
                Status = "Approved",
                Type = "Document",
                Priority = "High",
                OrderIndex = 1,
                DueDate = DateTime.UtcNow.AddDays(14)
            };

            context.ProjectTasks.AddRange(task1, task2);
            
            // 6. Add an availability slot for today/tomorrow if needed
            var availability = new AdviserAvailability
            {
                AdviserId = adviser.Id,
                AvailableDate = DateTime.Today,
                StartTime = new TimeSpan(14, 0, 0),
                EndTime = new TimeSpan(16, 0, 0),
                IsBooked = false
            };
            context.AdviserAvailabilities.Add(availability);

            await context.SaveChangesAsync();
            Console.WriteLine($"Successfully seeded Project '{project.ProjectName}' and availability for {adviser.FullName}");
        }
    }
}
