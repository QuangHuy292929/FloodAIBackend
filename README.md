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

## Cấu hình PostgreSQL trên Supabase

1. Trong Supabase Dashboard, mở project và chọn **Connect**. Lấy host, port, tên database và username từ **Direct connection**. Nếu mạng chỉ có IPv4, chọn **Session pooler** ở cổng `5432` và dùng username hiển thị cho chế độ đó.
2. Tải CA certificate từ **Database Settings → SSL Configuration** và lưu ở một đường dẫn trên máy.
3. Trong Visual Studio, nhấp phải project `FloodAI.Api` và chọn **Manage User Secrets**. Thêm khóa `ConnectionStrings:FloodAI` vào JSON hiện có. Điền thông tin PostgreSQL và đường dẫn chứng chỉ của bạn.

```json
{
  "ConnectionStrings:FloodAI": "Host=YOUR_HOST;Port=5432;Database=postgres;Username=YOUR_USERNAME;Password=YOUR_DATABASE_PASSWORD;SSL Mode=VerifyFull;Root Certificate=C:/path/to/supabase-ca.crt"
}
```

Dùng mật khẩu database của project Supabase. Lưu chuỗi kết nối trong User Secrets trên máy phát triển và cung cấp qua biến môi trường `ConnectionStrings__FloodAI` khi triển khai.

Để kiểm tra kết nối từ thư mục `backend`, chạy:

```powershell
dotnet ef migrations list --project FloodAI.DataAccess --startup-project FloodAI.Api -- --environment Development
```

Lệnh này đọc trạng thái migration. Khi kết nối thành công và database chưa có schema FloodAI, `InitialCreate` xuất hiện với trạng thái `Pending`.

[Hướng dẫn kết nối Supabase](https://supabase.com/docs/guides/database/connecting-to-postgres) · [Tham số kết nối Npgsql](https://www.npgsql.org/doc/connection-string-parameters)

## Migration đầu tiên

Migration `InitialCreate` đã được tạo trong project DataAccess. Khi đã kiểm tra kết nối và muốn áp dụng lên PostgreSQL, chạy lệnh sau.

```powershell
dotnet ef database update --project FloodAI.DataAccess --startup-project FloodAI.Api -- --environment Development
```

Project khởi động cung cấp cấu hình kết nối. Project DataAccess giữ migration và snapshot của schema.
