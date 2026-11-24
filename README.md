# Flood AI Backend

Backend ASP.NET Core Web API chạy trên .NET 10. Cơ sở dữ liệu dùng PostgreSQL và EF Core Code First.

| Project | Trách nhiệm |
| --- | --- |
| `FloodAI.Api` | HTTP API, controllers, cấu hình và đăng ký dịch vụ |
| `FloodAI.Business` | Nghiệp vụ |
| `FloodAI.DataAccess` | `FloodAiDbContext`, entity, cấu hình bảng và migrations |

## Chạy API

Chạy các lệnh sau trong thư mục `backend` với .NET 10 SDK.

```powershell
dotnet restore FloodAI.slnx
dotnet tool restore
dotnet build FloodAI.slnx --no-restore
dotnet run --project FloodAI.Api --launch-profile http
```

- `http://localhost:5289/health` kiểm tra tiến trình API. Endpoint này chạy được khi chưa cấu hình PostgreSQL.
- `http://localhost:5289/openapi/v1.json` trả tài liệu OpenAPI trong môi trường Development.

## Cấu hình PostgreSQL

Đặt chuỗi kết nối local bằng User Secrets. Thay tên tài khoản và mật khẩu bằng thông tin PostgreSQL của máy.

```powershell
dotnet user-secrets set 'ConnectionStrings:FloodAI' 'Host=localhost;Port=5432;Database=floodai;Username=YOUR_USER;Password=YOUR_PASSWORD' --project FloodAI.Api
```

Khi triển khai, cung cấp chuỗi kết nối qua biến môi trường `ConnectionStrings__FloodAI`.

## Tạo migration đầu tiên

Sau khi thêm entity và ánh xạ bảng vào `FloodAiDbContext`, tạo migration trong project DataAccess rồi áp dụng lên PostgreSQL.

```powershell
dotnet ef migrations add InitialCreate --project FloodAI.DataAccess --startup-project FloodAI.Api --output-dir Migrations
dotnet ef database update --project FloodAI.DataAccess --startup-project FloodAI.Api -- --environment Development
```

Project khởi động cung cấp cấu hình kết nối. Project DataAccess giữ migration và snapshot của schema.
