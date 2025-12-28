using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StudentPortal.Business.Implementation;
using StudentPortal.Business.Interface;
using StudentPortal.Data;
using StudentPortal.Data_Access.Repository.Implementation;
using StudentPortal.Data_Access.Repository.Interface;
using StudentPortal.Models;
using StudentPortal.Services.Implementations;
using StudentPortal.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// --- 1. KẾT NỐI DATABASE ---
// Lưu ý: Mở file appsettings.json xem chuỗi kết nối tên là "DefaultConnection" hay "StudentPortalContext" 
// rồi sửa dòng dưới đây cho khớp. Mình để mặc định là "DefaultConnection".
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<StudentPortalContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// --- 2. CẤU HÌNH IDENTITY (CHỈ DÙNG ĐOẠN NÀY) ---
// XÓA đoạn AddDefaultIdentity cũ đi
builder.Services.AddIdentity<User, IdentityRole<int>>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 3;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<StudentPortalContext>() // Kết nối đúng Context
.AddDefaultTokenProviders()
.AddDefaultUI();

// --- 3. ĐĂNG KÝ SERVICE & REPOSITORY ---
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages(); // Thêm dòng này nếu dùng Identity UI mặc định

// Đăng ký Repository Generic
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

// Đăng ký Business Services
builder.Services.AddScoped<IAnnouncementService, AnnouncementService>();
builder.Services.AddScoped<ICourseService, CourseService>();
builder.Services.AddScoped<IDepartmentService, DepartmentService>();
builder.Services.AddScoped<IFacultyService, FacultyService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IUserService, UserService>(); // Service quan trọng
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<IScheduleService, ScheduleService>();
builder.Services.AddScoped<IAdminManagementService, AdminManagementService>();
builder.Services.AddScoped<ICourseSectionService, CourseSectionService>();
builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();
builder.Services.AddScoped<ICourseMaterialService, CourseMaterialService>();
builder.Services.AddScoped<IScoreService, ScoreService>();

var app = builder.Build();

// --- 4. SEED DATA (KHỞI TẠO DỮ LIỆU MẪU) ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<StudentPortalContext>();
        var userManager = services.GetRequiredService<UserManager<User>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<int>>>();

        await DbInitSeed.InitializeAsync(context, userManager, roleManager);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Lỗi khi khởi tạo dữ liệu (Seeding Data).");
    }
}

// --- 5. MIDDLEWARE PIPELINE ---
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

// QUAN TRỌNG: Phải có Authentication trước Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();