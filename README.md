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

## Quan trắc Văn Úc

- `GET /api/v1/stations/{id}/metrics` liệt kê các đại lượng có dữ liệu tại trạm. Đơn vị chưa xác minh có giá trị `null`.
- `GET /api/v1/stations/{id}/observations?metric=water_level&from=2021-01-01&to=2021-01-02&page=1&pageSize=250` trả chuỗi giá trị gốc, nguồn và trạng thái chất lượng. `from` được tính vào kết quả, `to` là mốc loại trừ. Hai tham số dùng ngày ISO hoặc giờ ISO theo đồng hồ nguồn, ví dụ `2021-01-01T08:00:00`. Mỗi trang có tối đa 1.000 điểm và kèm `hasNextPage`.

Chạy lệnh ở thư mục `backend`. Kiểm tra CSV trước khi nhập:

```powershell
$csvPath = (Resolve-Path '..\datasets\processed\van_uc_hourly.csv').Path
dotnet run --project FloodAI.Api --launch-profile http -- --import-van-uc "$csvPath" --dry-run
```

Khi cả hai trạm đã được đăng ký trong lưu vực `van_uc`, thay `TRUNG_TRANG_ID` và `HON_DAU_ID` bằng ID của từng trạm rồi chạy:

```powershell
dotnet run --project FloodAI.Api --launch-profile http -- --import-van-uc "$csvPath" --trung-trang-id TRUNG_TRANG_ID --hon-dau-id HON_DAU_ID
```

Lệnh nhập dùng kết nối `ConnectionStrings:FloodAI` và ghi toàn bộ file trong một giao dịch. Kết quả kiểm tra kỳ vọng là 17.520 giờ và 52.560 giá trị. Chạy lại cùng tệp bỏ qua bản ghi đã có. Nếu gặp giá trị khác ở cùng nguồn, trạm, đại lượng và giờ, giao dịch được hủy để có thể kiểm tra sự khác biệt trước khi ghi.

## Dự báo lịch sử Văn Úc

Từ thư mục `backend`, tạo môi trường Python 3.13 cho dịch vụ inference và chạy dịch vụ trên loopback. Dịch vụ dùng ba model đã huấn luyện, manifest và hàm tạo đặc trưng trong thư mục `../ml`. Có thể chỉ định thư mục ML bằng biến `FLOODAI_ML_DIR`.

```powershell
python -m venv .venv-inference
.\.venv-inference\Scripts\python.exe -m pip install -r inference\requirements.txt
.\.venv-inference\Scripts\python.exe -m uvicorn inference.app:app --host 127.0.0.1 --port 8091
```

API gọi dịch vụ qua `ForecastInference:BaseUrl`, mặc định là `http://127.0.0.1:8091/`. Có thể cấu hình bằng biến môi trường `ForecastInference__BaseUrl`.

Sau khi đăng ký hai trạm và nhập CSV Văn Úc, mở terminal thứ hai. Thay `TRUNG_TRANG_ID` bằng ID thật rồi đăng ký ba model và ngưỡng nghiên cứu:

```powershell
dotnet run --project FloodAI.Api --launch-profile http -- --register-van-uc-forecast TRUNG_TRANG_ID
```

- `GET /api/v1/stations/{id}/models` trả danh sách mô hình Văn Úc đang hoạt động cho trạm.
- `POST /api/v1/forecasts/historical` nhận JSON với `stationId` và `issueTimeLocal`, ví dụ `2022-10-15T12:00:00`. Giờ phát hành theo đồng hồ dữ liệu nguồn và phải đúng đầu giờ.

API cần 48 giờ liên tiếp cho ba chuỗi đầu vào và trả dự báo mực nước Trung Trang ở `+1h`, `+3h`, `+6h`. Thực đo ở giờ mục tiêu được trả nếu có. Kết quả theo thang đo gốc và được đánh giá với **Q95 = 142**, ngưỡng nghiên cứu trên dữ liệu lịch sử. Mỗi yêu cầu hợp lệ gọi `POST /predict` của worker Python, kể cả khi trạm, giờ và dữ liệu đầu vào trùng với lượt trước. Backend lưu một lượt chạy mới cùng kết quả so ngưỡng trong PostgreSQL. Khi worker không khả dụng, API trả HTTP `503`.

## Tài khoản và phân quyền

Áp dụng migration `AddUserSessions` vào database mà bạn dùng để phát triển trước khi thử đăng nhập. Migration tạo bảng `user_sessions`.

| Endpoint | Quyền | Chức năng |
| --- | --- | --- |
| `POST /api/v1/auth/register` | Công khai | Đăng ký với `email`, `password`, `displayName`. Tài khoản mới có vai trò `USER`. |
| `POST /api/v1/auth/login` | Công khai | Đăng nhập bằng `email`, `password`. Trả `accessToken` và thời hạn phiên. |
| `POST /api/v1/auth/logout` | Đăng nhập | Thu hồi phiên hiện tại. |
| `GET /api/v1/me` và `PATCH /api/v1/me` | Đăng nhập | Xem hồ sơ hoặc đổi `displayName`. |
| `POST /api/v1/auth/change-password` | Đăng nhập | Gửi `currentPassword` và `newPassword`. Đổi mật khẩu thu hồi mọi phiên. |
| `POST /api/v1/auth/forgot-password` | Công khai | Gửi `email` để nhận token đặt lại mật khẩu khi SMTP đã được cấu hình. |
| `POST /api/v1/auth/reset-password` | Công khai | Dùng `token` trong email và `newPassword`. Token hết hạn sau 30 phút. |
| `PATCH /api/v1/admin/users/{id}/status` | `ADMIN` | Gửi `{"locked":true}` để khóa tài khoản `USER` hoặc `{"locked":false}` để mở khóa. |

Gửi token trong header `Authorization: Bearer <accessToken>` qua HTTPS. Phiên hết hạn sau 8 giờ, sau đó người dùng đăng nhập lại. Để cấp vai trò Admin cho một tài khoản đã đăng ký, chạy lệnh nội bộ trong thư mục `backend`:

```powershell
dotnet run --project FloodAI.Api --launch-profile https -- --promote-admin admin@example.com
```

Lệnh thu hồi phiên cũ khi đổi vai trò. Admin đăng nhập lại để nhận phiên mới.

Để bật email đặt lại mật khẩu, cấu hình `AuthSmtp:Host`, `AuthSmtp:Port`, `AuthSmtp:From` và thông tin `AuthSmtp:Username`, `AuthSmtp:Password` nếu máy chủ yêu cầu xác thực. Lưu thông tin xác thực trong User Secrets khi phát triển. Cổng mặc định là `587` với STARTTLS, cổng `465` dùng TLS ngay khi kết nối. Có thể đặt `Auth:DataProtectionKeysPath` nếu cần lưu khóa bảo vệ token ở thư mục riêng.

## Cộng đồng

Bài viết mới hoặc được cập nhật vào trạng thái `PENDING` để Admin duyệt trước khi lên bảng tin. Tác giả xem lại bài viết, bình luận và trạng thái trong các endpoint `/me`.

| Endpoint | Chức năng |
| --- | --- |
| `GET /api/v1/areas` | Danh mục khu vực hoạt động |
| `GET /api/v1/areas/{id}/posts` | Bảng tin công khai của khu vực và các khu vực con hoạt động |
| `GET /api/v1/posts/{id}` | Chi tiết bài công khai |
| `GET /api/v1/posts/{id}/comments` | Bình luận công khai của bài |
| `GET /api/v1/me/posts` và `GET /api/v1/me/posts/{id}` | Bài viết của người đăng nhập, kèm trạng thái và lý do từ chối nếu có |
| `GET /api/v1/me/comments` và `GET /api/v1/me/comments/{id}` | Bình luận của người đăng nhập |
| `POST /api/v1/posts`, `PATCH /api/v1/posts/{id}`, `DELETE /api/v1/posts/{id}` | Đăng bài, cập nhật trường được gửi hoặc xóa mềm bài của mình |
| `POST /api/v1/posts/{id}/comments`, `PATCH /api/v1/comments/{id}`, `DELETE /api/v1/comments/{id}` | Bình luận dưới bài công khai, sửa hoặc xóa mềm bình luận của mình |
| `POST /api/v1/posts/{id}/media` | Tải một ảnh qua `multipart/form-data` với trường `file` |
| `GET /api/v1/posts/{id}/media/{mediaId}`, `DELETE /api/v1/posts/{id}/media/{mediaId}` | Đọc ảnh theo quyền xem bài, hoặc xóa ảnh khỏi bài của mình |
| `POST /api/v1/content-reports` | Báo cáo đúng một bài hoặc bình luận công khai bằng `postId` hoặc `commentId` và `reason` |

Bảng tin hỗ trợ `type`, `from`, `to`, `page` và `pageSize`. Khoảng thời gian lọc theo ngày xuất bản, gồm `from` và loại trừ `to`. Trường `observedAt` và các mốc lọc dùng thời điểm ISO có múi giờ, ví dụ `2022-10-15T12:00:00+07:00`. Giá trị `postType` gồm `FLOOD_REPORT`, `WEATHER_UPDATE`, `DISCUSSION` và `OTHER`.

Mỗi bài có tối đa 5 ảnh JPEG, PNG hoặc WebP, mỗi ảnh tối đa 5 MiB. Backend lưu tệp riêng trên máy chạy API và kiểm tra quyền khi tải xuống. Đặt `CommunityMedia:RootPath` để chọn thư mục lưu trên máy chạy API. Mặc định là thư mục ứng dụng cục bộ của tài khoản chạy process, dưới `FloodAI/community-media`. DB hiện có hỗ trợ các endpoint này qua các bảng cộng đồng đã tạo.

### Kiểm duyệt và báo cáo

Bình luận được công khai ngay khi gửi vào bài đã xuất bản. Giới hạn tần suất ghi cộng đồng vẫn áp dụng. Sửa bình luận đang công khai giữ nguyên khả năng hiển thị. Sửa bình luận bị ẩn hoặc từ chối đưa nó về `PENDING` để Admin xem lại. Trong hồ sơ riêng, tác giả có thể xem `moderationReason` khi nội dung bị ẩn hoặc từ chối.

| Endpoint Admin | Chức năng |
| --- | --- |
| `GET /api/v1/admin/moderation/queue?kind=post` | Bài viết chờ duyệt. Dùng `kind=comment` để xem bình luận cần duyệt lại. |
| `POST /api/v1/admin/posts/{id}/{decision}` | Duyệt, từ chối hoặc ẩn bài. |
| `POST /api/v1/admin/comments/{id}/{decision}` | Duyệt, từ chối hoặc ẩn bình luận. |
| `GET /api/v1/admin/content-reports` | Báo cáo chờ xử lý, ưu tiên nội dung có nhiều người báo cáo. Có thể lọc `postId` hoặc `commentId`. |
| `POST /api/v1/admin/content-reports/{id}/resolve` | Gửi `{"resolution":"RESOLVED"}` hoặc `{"resolution":"DISMISSED"}`. |
| `GET /api/v1/admin/moderation/history` | Nhật ký quyết định, có thể lọc `postId` hoặc `commentId`. |

Dùng `approve`, `reject` hoặc `hide` cho `decision`. `reject` và `hide` cần JSON chứa `reason`. Báo cáo chỉ tạo việc cần xem xét. Admin quyết định ẩn dựa trên nội dung và lý do vi phạm.

## Theo dõi cá nhân

Các endpoint dưới đây yêu cầu Bearer token của người dùng. Dùng ID từ API danh mục khu vực hoặc trạm.

| Endpoint | Kết quả |
| --- | --- |
| `GET /api/v1/me/followed-areas` | Khu vực đang hoạt động mà tài khoản đang theo dõi |
| `PUT /api/v1/me/followed-areas/{areaId}` | Theo dõi khu vực |
| `DELETE /api/v1/me/followed-areas/{areaId}` | Bỏ theo dõi khu vực |
| `GET /api/v1/me/followed-areas/posts?page=1&pageSize=20` | Bài đã xuất bản ở các khu vực đang theo dõi và khu vực con đang hoạt động, mới nhất trước |
| `GET /api/v1/me/followed-stations` | Trạm đang theo dõi trong các lưu vực hoạt động, kèm ID để truy cập dữ liệu quan trắc |
| `PUT /api/v1/me/followed-stations/{stationId}` | Theo dõi trạm |
| `DELETE /api/v1/me/followed-stations/{stationId}` | Bỏ theo dõi trạm |

`PUT` và `DELETE` thành công trả `204`. Gửi lại `PUT` giữ thời điểm theo dõi ban đầu. `PUT` với đối tượng thiếu hoặc không hoạt động trả `404`.

## Quản trị danh mục

Các API bên dưới cần Bearer token của tài khoản `ADMIN`. Áp dụng migration `AddCatalogActions` vào cơ sở dữ liệu đích trước khi dùng các thao tác ghi và xem nhật ký.

| Endpoint | Chức năng |
| --- | --- |
| `GET`, `POST /api/v1/admin/catalog/{collection}`, `PUT /api/v1/admin/catalog/{collection}/{id}` | Liệt kê, thêm và cập nhật. Các collection là `basins`, `areas`, `stations`, `sources` và `metrics`. |
| `GET /api/v1/admin/catalog/observations` | Đọc giá trị và metadata, lọc theo `stationId` hoặc `qualityStatus`. |
| `PUT /api/v1/admin/catalog/observations/{id}/metadata` | Cập nhật `qualityStatus` và `availableAt`. |
| `GET /api/v1/admin/catalog/actions` | Xem nhật ký, có thể lọc `resourceType` như `BASIN` hoặc `FORECAST_BUNDLE`. |
| `GET /api/v1/admin/stations/{stationId}/models` | Xem phiên bản, kỳ dự báo, tệp mô hình và trạng thái đăng ký. |
| `POST /api/v1/admin/stations/{stationId}/models/register-van-uc` | Đăng ký bộ mô hình từ worker Văn Úc và ngưỡng Q95. Cần dữ liệu quan trắc đã nhập và worker đang chạy. |
| `PATCH /api/v1/admin/stations/{stationId}/models/{version}/status` | Bật hoặc tắt đồng thời các kỳ của một phiên bản. |
| `GET /api/v1/admin/stations/{stationId}/thresholds` | Xem ngưỡng, nguồn, tình trạng đơn vị và trạng thái kích hoạt. |
| `PATCH /api/v1/admin/stations/{stationId}/thresholds/{thresholdId}/status` | Bật hoặc tắt ngưỡng Q95 đã đăng ký. |

Khi cập nhật danh mục, gửi đầy đủ trường của đối tượng. Mã danh mục, lưu vực của trạm, tên và loại nguồn, và loại đại lượng của metric được cố định sau khi tạo. Dùng `isActive: false` để tắt lưu vực, khu vực hoặc nguồn. Trạm thuộc lưu vực đã tắt sẽ bị ẩn khỏi API danh mục công khai. Trạm và đại lượng hiện dùng cấu trúc dữ liệu gốc, không có trường trạng thái riêng.

Danh sách quan trắc và nhật ký nhận `page` và `pageSize`. Mỗi trang có tối đa 100 bản ghi. Chất lượng quan trắc gồm `SOURCE_UNVERIFIED`, `VALID`, `SUSPECT` và `INVALID`. Gửi `availableAt` theo ISO 8601 có múi giờ hoặc gửi `null` để xóa thời điểm. Giá trị và giờ gốc của nguồn được giữ nguyên. Dự báo lịch sử bỏ qua bản ghi `SUSPECT` và `INVALID` khi lấy đầu vào và thực đo để đối chiếu.

Các thao tác bật hoặc tắt dùng JSON `{"isActive":false}` hoặc `{"isActive":true}`. Đăng ký lại giữ trạng thái của bộ mô hình và ngưỡng đã có. Q95 bằng 142 theo thang đo gốc trong manifest và giữ nguồn gốc để đối chiếu các đánh giá đã lưu.
