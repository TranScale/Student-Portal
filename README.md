# Student Portal — Internal Learning Portal for Students

Hệ thống quản lý học tập nội bộ dành cho trường/trung tâm đào tạo, phục vụ 3 vai trò: **Sinh viên**, **Giảng viên**, **Quản trị viên (Admin)**. Ứng dụng xây dựng theo mô hình ASP.NET Core MVC, cho phép đăng ký học phần theo học kỳ, quản lý điểm, điểm danh, thời khóa biểu, tài liệu học phần, thông báo và thống kê học vụ.

## Mục lục

- [Tech stack](#tech-stack)
- [Kiến trúc & cấu trúc thư mục](#kiến-trúc--cấu-trúc-thư-mục)
- [Mô hình dữ liệu chính](#mô-hình-dữ-liệu-chính)
- [Chức năng theo vai trò](#chức-năng-theo-vai-trò)
- [Cài đặt & chạy project](#cài-đặt--chạy-project)
- [Tài khoản demo](#tài-khoản-demo)
- [Thành viên nhóm](#thành-viên-nhóm)

## Tech stack

| Thành phần | Công nghệ |
|---|---|
| Backend | ASP.NET Core 8.0 MVC (C#), Razor Views |
| ORM / Database | Entity Framework Core 8 (Code-First + Migrations), SQL Server (LocalDB) |
| Xác thực & phân quyền | ASP.NET Core Identity (`IdentityDbContext<User, IdentityRole<int>, int>`), phân quyền theo Role: Admin / Lecturer / Student |
| Import/Export Excel | EPPlus 8 |
| Frontend | Razor + Bootstrap, jQuery, jQuery Validation, FullCalendar (thời khóa biểu), Chart.js (biểu đồ thống kê), Easy-Pie-Chart, Sparkline, SimpleBar |
| Lưu file | Lưu trực tiếp vào `wwwroot/uploads/materials` (tài liệu học phần) và `wwwroot/images/avatars` (avatar người dùng) |

## Kiến trúc & cấu trúc thư mục

```
StudentPortal/
├── Controllers/            # Xử lý request, điều hướng theo vai trò (Role-based routing)
├── Business/
│   ├── Interface/          # Hợp đồng service nghiệp vụ (Enrollment, Score, Course, ...)
│   └── Implementation/     # Triển khai service nghiệp vụ
├── Data Access/
│   └── Repository/         # Repository Generic (IRepository<T>)
├── Data/
│   ├── StudentPortalContext.cs   # DbContext chính (kế thừa IdentityDbContext)
│   └── DbInitSeed.cs             # Seed dữ liệu mẫu (Khoa/Ngành/Môn/User/Học kỳ/Lớp...)
├── Models/                 # Entity (Student, Lecturer, Course, Enrollment, Score, ...)
├── ModelViews/             # ViewModel dùng riêng cho một số View (Attendance, Student, Lecturer)
├── Views/                  # Razor Views, tách theo layout riêng cho từng vai trò (_StudentLayout, _LecturerLayout, _AdminLayout)
├── Areas/Identity/         # Trang đăng nhập/đăng ký của ASP.NET Identity UI
└── wwwroot/                # Static assets, file upload
```

Luồng xử lý chuẩn: `Controller → Business Service → Data Access/Repository → StudentPortalContext (EF Core)`. Một số controller nghiệp vụ phức tạp (đăng ký học phần, điểm, điểm danh, thời khóa biểu, thống kê) truy vấn trực tiếp `StudentPortalContext` để tối ưu các câu query phức tạp (nhiều `Include`/`GroupBy`).

## Mô hình dữ liệu chính

```
User (Identity) 1─1 Student / Lecturer / Admin
Faculty 1─n Department 1─n Course 1─n CourseSection
Semester 1─n CourseSection
CourseSection n─n Student          (qua Enrollment)
CourseSection 1─n ScheduleItem     (buổi học cụ thể theo ngày)
CourseSection 1─n CourseMaterial   (tài liệu học phần)
Enrollment ←→ Score                (điểm quá trình/giữa kỳ/thi → điểm tổng kết)
ScheduleItem 1─n Attendee          (điểm danh theo buổi)
User 1─n Announcement, Certificate
```

Lịch học và lịch dạy dùng enum dạng bit-flag (`ClassDays`, `StudySessions`) để kiểm tra trùng lịch bằng toán tử `&`.

## Chức năng theo vai trò

### Sinh viên
- Dashboard: lịch học hôm nay, GPA tạm tính, tổng tín chỉ tích lũy.
- Đăng ký học phần theo học kỳ: cổng đăng ký mở/đóng theo khoảng thời gian trước ngày khai giảng, lọc theo khoa/ngành/từ khóa, tự kiểm tra sĩ số và trùng lịch trước khi đăng ký, hủy đăng ký.
- Xem thời khóa biểu theo tuần, điều hướng tuần trước/sau trong học kỳ.
- Xem bảng điểm theo học kỳ.
- Xem và tải tài liệu học phần (chỉ tài liệu được đánh dấu công khai).
- Quản lý chứng chỉ cá nhân (tạo/xem).
- Xem thông báo (lọc theo đối tượng nhận), có phân trang và tìm kiếm AJAX.
- Cập nhật hồ sơ cá nhân, đổi avatar.

### Giảng viên
- Đăng ký giờ dạy theo học kỳ (cùng cơ chế chống trùng lịch với sinh viên).
- Nhập điểm theo lớp: tự tính điểm tổng kết theo trọng số (quá trình 30% / giữa kỳ 20% / thi 50%) và xếp loại A–F; không cho nhập điểm khi học kỳ đã đóng.
- Điểm danh sinh viên theo từng buổi học (modal AJAX, lưu trạng thái có mặt/vắng + ghi chú).
- Quản lý tài liệu học phần: upload, xoá, chia sẻ công khai/riêng tư.
- Đăng thông báo cho sinh viên.
- Trang hồ sơ cá nhân kèm số liệu tổng số lớp/môn đã giảng dạy.

### Admin
- CRUD đầy đủ: Khoa, Ngành, Môn học, Lớp học phần, Học kỳ, Sinh viên, Giảng viên, Người dùng, Chứng chỉ, Thông báo.
- Quản lý danh sách đăng ký học phần: duyệt/đổi trạng thái (Pending/Approved/Finished), xoá đăng ký kèm xoá điểm liên quan.
- Import Excel hàng loạt danh sách sinh viên/giảng viên (chuẩn hoá tên Khoa/Ngành, tự tạo tài khoản Identity, gán Role, báo lỗi theo từng dòng).
- Trang thống kê học vụ: tổng số lớp/sinh viên/giảng viên hoạt động theo học kỳ, tỷ lệ qua môn, phân bố điểm A–F, top 5 sinh viên GPA cao nhất, top 5 giảng viên theo số lớp/tỷ lệ đậu, xu hướng rớt môn so với kỳ trước, danh sách lớp học có nguy cơ (tỷ lệ rớt ≥ 40% hoặc GPA trung bình < 5.0), tìm kiếm nhanh sinh viên/giảng viên.

## Cài đặt & chạy project

### Yêu cầu
- .NET 8 SDK
- SQL Server LocalDB (đi kèm Visual Studio) hoặc SQL Server bất kỳ

### Các bước

1. Clone repository và mở `Group 5-InternalLearningPortalforStudents.sln` (hoặc mở trực tiếp thư mục `StudentPortal`).
2. Kiểm tra chuỗi kết nối trong `appsettings.json` (mục `ConnectionStrings:DefaultConnection`), mặc định dùng LocalDB:
   ```
   Server=(localdb)\mssqllocaldb;Database=StudentPortal;Trusted_Connection=True;MultipleActiveResultSets=true
   ```
3. Áp dụng migration để tạo database:
   ```
   dotnet ef database update
   ```
4. Chạy project:
   ```
   dotnet run
   ```
5. Lần chạy đầu tiên, `DbInitSeed` sẽ tự động seed dữ liệu mẫu (Khoa, Ngành, Môn học, Học kỳ, tài khoản Admin/Giảng viên/Sinh viên, lớp học phần, thời khóa biểu, thông báo) nếu database chưa có dữ liệu Khoa.

## Tài khoản demo

Mật khẩu chung cho tất cả tài khoản seed: `Student@123`

| Vai trò | Tài khoản |
|---|---|
| Admin | `admin` |
| Giảng viên | `gv01`, `gv02`, `gv03` |
| Sinh viên | `sv01`, `sv02`, `sv03`, `sv04`, `sv05` |

## Thành viên nhóm

Nhóm 5 — Internal Learning Portal for Students. Danh sách đóng góp theo lịch sử commit:

- TranScale
- hnp8uvnb
- Vanlk22 / vanlk22-sudo
