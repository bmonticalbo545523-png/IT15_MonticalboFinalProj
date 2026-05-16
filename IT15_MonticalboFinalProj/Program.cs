using IT15_MonticalboFinalProj.Data;
using IT15_MonticalboFinalProj.Models;
using IT15_MonticalboFinalProj.Repositories;
using IT15_MonticalboFinalProj.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Identity with roles
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Cookie config
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
});

// Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IProjectRepository, ProjectRepository>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
builder.Services.AddScoped<IProjectTaskRepository, ProjectTaskRepository>();
builder.Services.AddScoped<IProjectMemberRepository, ProjectMemberRepository>();
builder.Services.AddScoped<IAnnouncementRepository, AnnouncementRepository>();
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IDeliverableSubmissionRepository, DeliverableSubmissionRepository>();
builder.Services.AddScoped<IDeliverableCommentRepository, DeliverableCommentRepository>();
builder.Services.AddScoped<IDepartmentRepository, DepartmentRepository>();
builder.Services.AddScoped<IProjectMilestoneRepository, ProjectMilestoneRepository>();
builder.Services.AddScoped<IProjectStageRepository, ProjectStageRepository>();
builder.Services.AddScoped<IResearchDocumentRepository, ResearchDocumentRepository>();
builder.Services.AddScoped<IProjectMessageRepository, ProjectMessageRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<IDefenseScheduleRepository, DefenseScheduleRepository>();

// MVC + Session
builder.Services.AddHttpContextAccessor();
builder.Services.AddControllersWithViews();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Recaptcha Settings & Service
builder.Services.Configure<IT15_MonticalboFinalProj.Models.RecaptchaSettings>(builder.Configuration.GetSection("RecaptchaSettings"));
builder.Services.AddHttpClient<IT15_MonticalboFinalProj.Services.RecaptchaService>();

var app = builder.Build();

// Seed database
try
{
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        await DbSeeder.SeedAsync(services);
    }
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "An error occurred during database seeding/migration. The app will continue to start, but database-dependent features may fail.");
}

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();

// Deactivation enforcement middleware
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value?.ToLower() ?? "";
    // Skip for account actions and static files to prevent loops or overhead
    if (path.StartsWith("/account") || path.StartsWith("/lib") || path.StartsWith("/css") || path.StartsWith("/js"))
    {
        await next();
        return;
    }

    if (context.User.Identity?.IsAuthenticated == true)
    {
        // Never block SuperAdmin
        if (context.User.IsInRole("SuperAdmin"))
        {
            await next();
            return;
        }

        var userManager = context.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.GetUserAsync(context.User);
        if (user != null)
        {
            bool shouldBlock = !user.IsActive || user.Status == "Inactive";

            if (!shouldBlock && user.DepartmentId.HasValue)
            {
                var deptRepo = context.RequestServices.GetRequiredService<IDepartmentRepository>();
                var dept = await deptRepo.GetByIdAsync(user.DepartmentId.Value);
                if (dept != null && !dept.IsActive) shouldBlock = true;
            }

            if (shouldBlock)
            {
                var signInManager = context.RequestServices.GetRequiredService<SignInManager<ApplicationUser>>();
                await signInManager.SignOutAsync();
                context.Response.Redirect("/Account/Login?error=AccountDeactivated");
                return;
            }
        }
    }
    await next();
});

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
