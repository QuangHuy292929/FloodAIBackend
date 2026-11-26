# Bàn giao FloodAI cho frontend

Frontend gọi API ASP.NET Core dưới prefix `/api/v1`. Backend đọc PostgreSQL và gọi worker Python khi có yêu cầu dự báo. Frontend chỉ cần URL của API và tài khoản thử nếu tích hợp tính năng yêu cầu đăng nhập.

- [Hợp đồng API theo chức năng](api-reference.md) ghi endpoint, quyền, payload và quy tắc hiển thị.
- [OpenAPI JSON](openapi.v1.json) là bản xuất từ backend để nhập vào công cụ API hoặc sinh kiểu dữ liệu. Dùng tài liệu hợp đồng để xem quyền, mã thành công và các quy tắc nghiệp vụ chưa thể hiện đầy đủ trong bản xuất.
- [README backend](../README.md) có hướng dẫn .NET, PostgreSQL và cấu hình worker.

## Chạy và kết nối

| Thành phần | Cấu hình local |
| --- | --- |
| API .NET 10 | `http://localhost:5289` với profile `http` |
| API HTTPS | `https://localhost:7202` với profile `https` |
| Kiểm tra tiến trình API | `GET /health` |
| OpenAPI khi API chạy ở Development | `GET /openapi/v1.json` |
| Worker Python 3.13 | Backend gọi `http://127.0.0.1:8091/` theo mặc định |

Nếu team backend cung cấp URL API, dùng URL đó và bỏ qua phần khởi động local. Với máy tự chạy, người vận hành backend cấu hình kết nối PostgreSQL qua User Secrets của `FloodAI.Api` theo README. Giữ thông tin kết nối PostgreSQL và SMTP ở phía server. Khi dùng Supabase demo hiện có, ba migration, dữ liệu Văn Úc và bộ mô hình đã được đăng ký. Database mới cần các bước chuẩn bị dữ liệu theo README.

Từ thư mục `backend`, cài môi trường worker một lần rồi chạy ở terminal thứ nhất. Giữ thư mục `ml` bên cạnh `backend` để worker tìm được mô hình.

```powershell
python -m venv .venv-inference
.\.venv-inference\Scripts\python.exe -m pip install -r inference\requirements.txt
.\.venv-inference\Scripts\python.exe -m uvicorn inference.app:app --host 127.0.0.1 --port 8091
```

Ở terminal thứ hai, chạy API.

```powershell
dotnet restore FloodAI.slnx
dotnet run --project FloodAI.Api --launch-profile http
```

Profile HTTP dùng cho loopback local. Dùng HTTPS cho môi trường chia sẻ, đặc biệt với request mang Bearer token. `/health` chỉ xác nhận tiến trình .NET phản hồi. Để kiểm tra dữ liệu và worker, gọi danh mục rồi thử endpoint dự báo.

Khi hợp đồng API thay đổi, khởi động profile Development và chạy lệnh sau từ thư mục `backend` để xuất lại OpenAPI.

```powershell
Invoke-WebRequest http://localhost:5289/openapi/v1.json -OutFile docs/openapi.v1.json
```

### Khi frontend chạy khác cổng

Backend hiện chưa cấu hình CORS. Dùng proxy ở dev server để trình duyệt gọi `/api/v1/...` cùng origin. Ví dụ cấu hình Vite cho profile HTTP local.

```js
export default {
  server: {
    proxy: {
      "/api": "http://localhost:5289",
    },
  },
};
```

Khi triển khai, phục vụ frontend và chuyển tiếp `/api` đến backend qua cùng origin, hoặc thống nhất origin được phép với người vận hành backend.

## Chọn khu vực Weather

Gọi `GET /api/v1/areas?hasWeatherLocation=true` để lấy các khu vực có tọa độ đại diện. Gửi thêm `search` để lọc theo tên hoặc mã. Lưu `id` của khu vực được chọn để dùng với các API Weather các gói tiếp theo. Tọa độ trả về là của khu vực trong danh mục.

Migration `AddAreaWeatherLocation` điền tọa độ cho khu vực `hai_phong` đã có. Với cơ sở dữ liệu mới, Admin tạo khu vực rồi gọi API cấu hình tọa độ. Tọa độ đại diện được làm tròn từ điểm Hải Phòng trong danh mục địa danh. [Nguồn tọa độ Hải Phòng](https://assets.publishing.service.gov.uk/media/6a3a740deaee00074150f25f/Vietnam_Toponymic_Factfile.pdf)

Sau khi chọn khu vực, gọi `GET /api/v1/weather/current?areaId=1` để lấy thời tiết Hải Phòng. Dùng `validAt` làm mốc dữ liệu, `timeZone` để định dạng giờ địa phương, và hiển thị nguồn từ `source` và `sourceUrl`. Xem [hợp đồng API](api-reference.md) để biết kiểu dữ liệu và trạng thái lỗi.

Gọi tiếp `GET /api/v1/weather/forecast?areaId=1` để lấy `hourly` cho biểu đồ theo giờ và `daily` cho danh sách 7 ngày. Dùng `validAt` làm mốc giờ và giữ `daily.date` theo lịch Việt Nam. Chuỗi theo giờ bắt đầu từ nửa đêm hôm nay. Nếu cần, lọc các mốc đã qua ở giao diện.

## Thử luồng dự báo Văn Úc

Dữ liệu demo trên Supabase có lưu vực Văn Úc `1`, khu vực Hải Phòng `1`, trạm Trung Trang `1` và Hòn Dấu `2`. Dùng danh mục API để lấy ID trong từng môi trường.

1. Gọi `GET /api/v1/basins`, rồi `GET /api/v1/basins/{id}/stations`.
2. Lấy danh sách đại lượng qua `GET /api/v1/stations/{id}/metrics`. Gọi `GET /api/v1/stations/{id}/observations` để vẽ thực đo.
3. Với Trung Trang, gọi `GET /api/v1/stations/1/models`. Bật nút **Dự báo** khi có các kỳ 1, 3 và 6 giờ.
4. Khi bấm **Dự báo**, gửi một request qua `POST /api/v1/forecasts/historical`. Hiển thị trạng thái đang tính cho đến khi API trả kết quả. Mỗi request hợp lệ gọi mô hình và tạo `runId` riêng.
5. Vẽ chuỗi dự báo và thực đo theo `targetTimeLocal`. Giữ điểm thực đo thiếu ở dạng `null`. Hiển thị phiên bản mô hình, ngưỡng nghiên cứu Q95 và trạng thái đơn vị từ phản hồi.

Ví dụ gọi API từ frontend qua proxy.

```js
const response = await fetch("/api/v1/forecasts/historical", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify({
    stationId: 1,
    issueTimeLocal: "2022-10-15T12:00:00",
  }),
});

if (!response.ok) {
  const problem = await response.json().catch(() => null);
  throw new Error(problem?.detail ?? problem?.title ?? `HTTP ${response.status}`);
}

const forecast = await response.json();
console.table(forecast.predictions);
```

Với giờ thử trên và bộ `backtest-2022-h2`, dự báo dự kiến lần lượt là `134.64`, `92.51`, `37.65` và thực đo là `132`, `89`, `34`. Cả ba dưới Q95 `142`. Giờ được hiển thị theo dữ liệu nguồn, đơn vị chưa xác minh và ngưỡng chỉ dùng để nghiên cứu.

## Kiểm tra các màn hình có đăng nhập

- Đăng ký rồi đăng nhập. Lấy `user.role` để quyết định hiển thị các chức năng quản trị. API tự kiểm tra quyền cho mỗi request.
- Tạo bài với khu vực Hải Phòng, kiểm tra trạng thái `PENDING` ở **Bài của tôi**, rồi dùng tài khoản ADMIN để duyệt.
- Sau khi bài xuất bản, thử bình luận, gửi báo cáo và theo dõi khu vực. Xác nhận bài xuất hiện trong **Bảng tin theo dõi**.
- Khi thử quên mật khẩu, xác nhận SMTP đã được cấu hình. Luồng dùng mã đặt lại đã qua kiểm thử tích hợp. Gửi email qua SMTP thật còn cần kiểm tra.

Các request mẫu định dạng HTTP có sẵn trong [FloodAI.Api.http](../FloodAI.Api/FloodAI.Api.http).
