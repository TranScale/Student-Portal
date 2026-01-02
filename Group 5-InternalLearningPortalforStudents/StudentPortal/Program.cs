using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StudentPortal.Business.Implementation;
using StudentPortal.Business.Interface;
using StudentPortal.Data;
using StudentPortal.Data_Access.Repository.Implementation;
using StudentPortal.Data_Access.Repository.Interface;
using StudentPortal.Services.Implementations;
using StudentPortal.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// DB của portal
builder.Services.AddDbContext<StudentPortalContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("StudentPortalContext")
        ?? throw new InvalidOperationException("Connection string 'StudentPortalContext' not found.")
    ));

// DB của Identity
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Identity + Roles
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllersWithViews();

// Cookie paths cho Identity
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
});

// DI repository + services
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

builder.Services.AddScoped<IAnnouncementService, AnnouncementService>();
builder.Services.AddScoped<ICourseService, CourseService>();
builder.Services.AddScoped<IDepartmentService, DepartmentService>();
builder.Services.AddScoped<IFacultyService, FacultyService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<IScheduleService, ScheduleService>();
builder.Services.AddScoped<IAdminManagementService, AdminManagementService>();
builder.Services.AddScoped<ICourseSectionService, CourseSectionService>();
builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();
builder.Services.AddScoped<ICourseMaterialService, CourseMaterialService>();
builder.Services.AddScoped<IScoreService, ScoreService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
    var portalDb = services.GetRequiredService<StudentPortalContext>();

    var roles = new[] { "Admin", "Student", "Lecturer" };
    foreach (var r in roles)
        if (!await roleManager.RoleExistsAsync(r))
            await roleManager.CreateAsync(new IdentityRole(r));

    async Task EnsureUser(string email, string password, string role)
    {
        var u = await userManager.FindByNameAsync(email);

        if (u == null)
        {
            u = new IdentityUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            var rs = await userManager.CreateAsync(u, password);
            if (!rs.Succeeded)
                throw new Exception(string.Join(" | ", rs.Errors.Select(e => e.Description)));
        }

        if (!await userManager.IsInRoleAsync(u, role))
        {
            var addRs = await userManager.AddToRoleAsync(u, role);
            if (!addRs.Succeeded)
                throw new Exception("AddToRole failed: " + string.Join(" | ", addRs.Errors.Select(e => e.Description)));
        }
    }

    // ✅ khai báo EnsurePortalUser NGAY TRONG CÙNG BLOCK => chắc chắn "thấy"
    async Task EnsurePortalUser(string email)
    {
        // IMPORTANT: đổi field cho đúng model User portal của mày
        var exists = await portalDb.Users.AnyAsync(u => u.Email == email);
        if (!exists)
        {
            portalDb.Users.Add(new StudentPortal.Models.User
            {
                Email = email,
                UserName = email,
                FullName = email // nếu FullName bắt buộc
            });

            await portalDb.SaveChangesAsync();
        }
    }

    await EnsureUser("admin@sp.com", "Admin@12345", "Admin");
    await EnsureUser("sv01@sp.com", "Student@12345", "Student");
    await EnsureUser("gv01@sp.com", "Lecturer@12345", "Lecturer");

    // ✅ đảm bảo Portal DB có user tương ứng email
    await EnsurePortalUser("admin@sp.com");
    await EnsurePortalUser("sv01@sp.com");
    await EnsurePortalUser("gv01@sp.com");
}



// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Seed dữ liệu StudentPortalContext
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<StudentPortalContext>();
        DbInitSeed.Initialize(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Đã xảy ra lỗi khi khởi tạo dữ liệu (Seeding DB).");
    }
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();
