# Quản lý chi tiêu cá nhân

Ứng dụng web quản lý tài chính cá nhân, được xây dựng bằng **ASP.NET Core MVC .NET 10**, **Entity Framework Core** và **SQL Server**. Người dùng có thể ghi nhận thu chi, quản lý ví, phân loại giao dịch, lập ngân sách và theo dõi báo cáo.

Dự án được chuyển đổi từ ASP.NET MVC 5 sang một solution ASP.NET Core MVC độc lập, tổ chức theo cấu trúc MVC trong một project.

## Chức năng chính

- **Tài khoản:** đăng ký, đăng nhập, đăng xuất, đổi mật khẩu và đặt lại mật khẩu sau khi nhập email đã đăng ký.
- **Ví tiền:** quản lý ví tiền mặt, ngân hàng và ví điện tử; theo dõi số dư.
- **Danh mục:** quản lý danh mục thu/chi, hỗ trợ danh mục cha và con.
- **Giao dịch:** thêm, sửa, xóa thu nhập, chi tiêu và chuyển tiền giữa các ví; lọc theo loại, danh mục, ví, khoảng ngày và phân trang.
- **Ngân sách:** đặt hạn mức theo danh mục và tháng/năm, theo dõi số tiền đã chi và tỷ lệ sử dụng.
- **Tổng quan:** thống kê thu chi tháng hiện tại, biểu đồ theo danh mục và tháng, giao dịch gần nhất và ngân sách.
- **Báo cáo:** tổng hợp thu chi và chi tiết giao dịch theo khoảng thời gian.

Ứng dụng hiện cung cấp chức năng cho **người dùng cá nhân**, chưa có khu vực quản trị Admin riêng. Dữ liệu cá nhân được kiểm tra và truy xuất theo tài khoản đang đăng nhập.

## Công nghệ

| Thành phần | Công nghệ |
| --- | --- |
| Nền tảng | .NET 10, ASP.NET Core MVC |
| Giao diện | Razor Views, Bootstrap 4, jQuery, font Rubik, phong cách Ekash |
| Biểu đồ | Chart.js |
| Truy cập dữ liệu | Entity Framework Core 10, Code First và Migrations |
| Cơ sở dữ liệu | SQL Server |
| Xác thực | Cookie Authentication, băm mật khẩu BCrypt |
| Bảo vệ biểu mẫu | Antiforgery, kiểm tra đầu vào và quyền sở hữu dữ liệu |

Các bảng nghiệp vụ chính gồm **Users, Categories, Wallets, Transactions và Budgets**. Thao tác cập nhật giao dịch đồng thời điều chỉnh số dư ví và dữ liệu ngân sách liên quan.

## Yêu cầu cài đặt

- **.NET 10 SDK** để restore, build và chạy mã nguồn.
- **SQL Server** đã được cài đặt, đang hoạt động và có thể kết nối. Có thể dùng bản Express hoặc Developer.
- **Visual Studio hỗ trợ .NET 10** với workload ASP.NET and web development nếu chạy bằng IDE; hoặc sử dụng terminal với .NET CLI.
- **SQL Server Management Studio (SSMS)** nếu cần quản lý database hoặc chạy file SQL khởi tạo dữ liệu.

EF Core tạo database và các bảng trên SQL Server đã cấu hình; ứng dụng không cài đặt dịch vụ SQL Server.

## Cài đặt và chạy

Giải nén hoặc clone repository, sau đó mở terminal tại thư mục chứa `QuanLyChiTieuCaNhan.csproj`. Thực hiện lần lượt các bước sau.

### 1. Cấu hình SQL Server

Sửa phần `ConnectionStrings` trong `appsettings.json`. Ví dụ dùng Windows Authentication và SQL Server mặc định trên máy:

```json
{
  "ConnectionStrings": {
    "AppDbContext": "Server=localhost;Database=QuanLyChiTieuCaNhan_Core;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True"
  }
}
```

Đây là phần cấu hình cần chỉnh; giữ các mục cấu hình khác trong file. Source hiện dùng khóa **AppDbContext** trong `Program.cs` và `appsettings.json`.

- Thay `Server` bằng đúng tên server/instance đang kết nối được trong SSMS. Ví dụ named instance trong JSON: `Server=.\\SQLEXPRESS` hoặc `Server=.\\MSSQLSERVER03`.
- Thay `Database` bằng tên database muốn sử dụng.
- Nếu dùng SQL Authentication, thay `Trusted_Connection=True` bằng `User Id=<SQL_LOGIN>;Password=<SQL_PASSWORD>` với tài khoản hợp lệ.
- Tài khoản kết nối cần quyền tạo database khi database chưa tồn tại, hoặc quyền áp dụng migration trên database đã có.
- Nếu bản source của bạn đã đổi `GetConnectionString("AppDbContext")` sang `GetConnectionString("DefaultConnection")`, khóa trong JSON cũng phải đổi tương ứng.

### 2. Khôi phục NuGet và build

```powershell
dotnet restore
dotnet build QuanLyChiTieuCaNhan.sln
```

### 3. Khởi tạo cơ sở dữ liệu

Chạy sau khi đã cấu hình đúng kết nối:

```powershell
dotnet run -- --initialize-database
```

Lệnh này áp dụng EF Core Migrations, tạo database nếu chưa có và tài khoản kết nối đủ quyền, tạo/cập nhật các bảng, rồi thêm danh mục mặc định. Sau khi hoàn tất, chương trình **thoát**.

**Chạy web bình thường không tự áp dụng migration.** Cần thực hiện bước này trước lần chạy đầu tiên và khi cần áp dụng migration mới. Lệnh khởi tạo không tạo tài khoản người dùng.

### 4. Chạy ứng dụng

```powershell
dotnet run --launch-profile http
```

Mở [http://localhost:5029](http://localhost:5029). Trang đăng nhập: [http://localhost:5029/Account/Login](http://localhost:5029/Account/Login). Bạn có thể đăng ký tài khoản mới từ giao diện.

Để chạy trong Visual Studio: mở `QuanLyChiTieuCaNhan.sln`, chọn project Web làm Startup Project nếu cần, chọn profile `http` rồi nhấn **F5**. Bước cấu hình và khởi tạo database vẫn cần hoàn tất trước đó.

Profile `https` sử dụng [https://localhost:7004](https://localhost:7004):

```powershell
dotnet run --launch-profile https
```

## Nạp dữ liệu khởi tạo (tùy chọn)

Sau khi database đã được khởi tạo, mở [Data/Seed.SampleData.sql](Data/Seed.SampleData.sql) trong SSMS. Kiểm tra dòng `USE [...]` đầu file và sửa cho **khớp tên database trong connection string**, sau đó chạy toàn bộ file.

Script bổ sung danh mục, ví, thu nhập, 13 khoản chi và ngân sách cho tài khoản:

| Email | Mật khẩu khi tạo mới |
| --- | --- |
| `phonght010181@tvu-onschool.edu.vn` | `123456` |

Nếu email đã tồn tại, script giữ nguyên mật khẩu và thông tin tài khoản hiện có. Script kiểm tra dữ liệu đã nạp để tránh nhân đôi giao dịch khi chạy lại.

Bạn có thể bỏ qua bước này và tự nhập dữ liệu từ giao diện sau khi đăng ký.

## Khôi phục mật khẩu

Mở **Quên mật khẩu**, nhập email đã đăng ký rồi chọn **Tiếp tục**. Nếu tài khoản tồn tại và chưa bị xóa, ứng dụng chuyển đến **Đặt lại mật khẩu**. Nhập mật khẩu mới và xác nhận để hoàn tất, sau đó đăng nhập lại.

Luồng này không gửi email, không dùng token khôi phục và không có thời hạn liên kết 30 phút. Tài khoản cần khôi phục được giữ bằng TempData được bảo vệ, không lấy từ email gửi lên ở biểu mẫu mật khẩu mới. Thao tác thành công mở khóa tài khoản và đặt số lần đăng nhập sai về 0.

**Giới hạn bảo mật:** chỉ biết email là có thể đặt lại mật khẩu; luồng này không xác minh quyền sở hữu email. Cần xác minh danh tính trước khi dùng cho website công khai có dữ liệu thực.

## Cấu trúc dự án

```text
QuanLyChiTieuCaNhan/
├── .config/                 # Manifest công cụ dotnet-ef
├── Controllers/             # Xử lý request, dữ liệu và quyền truy cập
├── Data/
│   ├── Migrations/          # Lịch sử thay đổi schema
│   ├── AppDbContext.cs      # Ánh xạ thực thể, quan hệ và chỉ mục
│   ├── DbInitializer.cs     # Khởi tạo danh mục
│   ├── LegacyDataImporter.cs
│   └── Seed.SampleData.sql
├── Models/                  # User, Category, Wallet, Transaction, Budget
├── Properties/              # Profile chạy HTTP/HTTPS
├── Services/                # Claims và khôi phục mật khẩu
├── ViewModels/              # Dữ liệu biểu mẫu và màn hình
├── Views/                   # Razor Views theo controller và Shared
├── wwwroot/                 # CSS, JavaScript, font, icon và hình ảnh
├── appsettings.json
├── appsettings.Development.json
├── Program.cs               # DI, xác thực, middleware và routing
├── QuanLyChiTieuCaNhan.csproj
├── QuanLyChiTieuCaNhan.sln
└── README.md
```

`bin/` và `obj/` do quá trình build tạo ra, có thể bỏ khi đóng gói source. Database cần được tạo, nhập dữ liệu hoặc sao lưu/khôi phục riêng khi chuyển máy.

## Nhập dữ liệu từ MVC 5 (tùy chọn)

Chỉ dùng khi cần chuyển dữ liệu cũ. Database đích phải **rỗng, khác database nguồn và có tên kết thúc bằng `_Core`**. Không nạp dữ liệu khởi tạo hoặc đăng ký tài khoản vào database đích trước khi nhập.

Cấu hình kết nối đích trong `appsettings.json`, rồi cung cấp kết nối nguồn bằng biến môi trường:

```powershell
$env:LegacyImport__ConnectionString = 'Server=;Database=QuanLyChiTieuCaNhan;Trusted_Connection=True;TrustServerCertificate=True'
dotnet run -- --import-legacy
Remove-Item Env:LegacyImport__ConnectionString
```

