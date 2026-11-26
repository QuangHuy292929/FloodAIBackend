# Hợp đồng API FloodAI

Các đường dẫn bên dưới bắt đầu bằng `/api/v1`. Xem [hướng dẫn tích hợp](frontend-handoff.md) để chọn URL theo môi trường, hoặc nhập [OpenAPI JSON](openapi.v1.json) để tra schema.

## Quy ước chung

- JSON dùng camelCase. Request có body JSON dùng `Content-Type: application/json`. Upload ảnh dùng `multipart/form-data`.
- Endpoint ghi **Đăng nhập** hoặc **ADMIN** cần header `Authorization: Bearer <accessToken>`. Token là chuỗi phiên, thời hạn 8 giờ. Dùng `expiresAt` để biết khi nào cần đăng nhập lại.
- `200` và `201` thường trả JSON, ngoại trừ tải ảnh trả dữ liệu nhị phân. `202` ở luồng quên mật khẩu và `204` có thể trả body rỗng. Kiểm tra status trước khi giải mã JSON.
- Danh sách có phân trang trả `{ "page": 1, "pageSize": 20, "hasNextPage": false, "items": [] }`. Trang bắt đầu từ 1. Chỉ lấy trang kế tiếp nếu `hasNextPage` là `true`. Danh mục thông thường trả mảng JSON. `pageSize` mặc định/tối đa là 20/50 cho cộng đồng, theo dõi và kiểm duyệt, 20/100 cho nhật ký và quan trắc Admin, 250/1.000 cho quan trắc công khai.

| Mã lỗi | Frontend xử lý |
| --- | --- |
| `400` | Dữ liệu không hợp lệ. Hiển thị lỗi theo trường nếu có `errors`. |
| `401` | Đăng nhập thất bại hoặc phiên thiếu, hết hạn hay bị thu hồi. Với endpoint yêu cầu đăng nhập, chuyển về màn hình đăng nhập. |
| `403` | Tài khoản thiếu quyền, ví dụ tài khoản USER truy cập API quản trị. |
| `404` | Tài nguyên thiếu, không công khai hoặc người dùng hiện tại không thể thấy. |
| `409` | Xung đột như email trùng, quá số ảnh, trạng thái kiểm duyệt đã đổi hoặc khóa danh mục bị trùng. |
| `422` | Chưa đủ điều kiện xử lý. Ví dụ khu vực chưa có tọa độ Weather, hoặc dự báo lịch sử thiếu dữ liệu, mô hình hay Q95. |
| `429` | Quá tần suất. Chờ rồi thử lại. Các nhóm xác thực có giới hạn, ghi cộng đồng và dự báo có chính sách riêng, mỗi chính sách 30 yêu cầu/phút/IP. |
| `503` | Nguồn thời tiết, dịch vụ dự báo hoặc email đặt lại mật khẩu không khả dụng. |

Lỗi thường theo `ProblemDetails` với `status`, `title`, `detail` nếu có. Lỗi xác thực đầu vào có thể thêm `errors` chứa mảng thông báo theo tên trường. Chuẩn bị thông báo mặc định nếu phản hồi lỗi có body rỗng.

## Thời tiết hiện tại

`GET /api/v1/weather/current?areaId=1` công khai và trả `200` cho khu vực hoạt động có tọa độ. Lấy ID từ `GET /api/v1/areas?hasWeatherLocation=true`. Thiếu hoặc sai `areaId` trả `400`. Khu vực không tồn tại hoặc bị tắt trả `404`. Khu vực chưa có tọa độ trả `422`. Khi Open-Meteo lỗi, quá thời gian chờ hoặc không trả được dữ liệu hợp lệ, API trả `503`.

Ví dụ phản hồi cho Hải Phòng.

```json
{
  "areaId": 1,
  "areaName": "Hải Phòng",
  "latitude": 20.86472,
  "longitude": 106.68333,
  "source": "Open-Meteo",
  "sourceUrl": "https://open-meteo.com/",
  "dataOrigin": "WEATHER_PROVIDER",
  "timeZone": "Asia/Ho_Chi_Minh",
  "validAt": "2026-11-26T16:15:00+00:00",
  "fetchedAt": "2026-11-26T16:18:00+00:00",
  "intervalSeconds": 900,
  "temperatureC": 19.8,
  "apparentTemperatureC": 21.7,
  "relativeHumidityPercent": 83,
  "precipitationMm": 0,
  "windSpeedKmh": 3.1,
  "windDirectionDegrees": 62,
  "weatherCode": 2,
  "isDay": false
}
```

- `validAt` là mốc dữ liệu của nhà cung cấp. `fetchedAt` là thời điểm backend lấy được dữ liệu. Cả hai là thời điểm ISO 8601 có UTC offset. Dùng `timeZone` để hiển thị giờ Việt Nam.
- `intervalSeconds` là độ dài khoảng thời gian từ nhà cung cấp. Lượng mưa là tổng trong khoảng ngay trước `validAt`. Dữ liệu thời tiết được tính từ mô hình khí tượng của Open-Meteo. Ghi nguồn trên giao diện. [Tài liệu Open-Meteo](https://open-meteo.com/en/docs#current-weather)
- Các giá trị thời tiết từ `temperatureC` đến `isDay` có thể là `null` nếu nhà cung cấp thiếu giá trị. Giữ `0` là giá trị hợp lệ đối với lượng mưa. Dùng `weatherCode` để ánh xạ biểu tượng thời tiết theo bảng WMO của nhà cung cấp. [Bảng mã thời tiết](https://open-meteo.com/en/docs#api-documentation)

## Dự báo theo giờ và 7 ngày

`GET /api/v1/weather/forecast?areaId=1` là API công khai nhận ID khu vực hoạt động có tọa độ Weather. Mã phản hồi gồm `200`, `400`, `404`, `422` và `503`, cùng ý nghĩa với API thời tiết hiện tại. Phản hồi lỗi có dạng ProblemDetails.

| Phần phản hồi | Trường |
| --- | --- |
| Thông tin khu vực và nguồn | `areaId, areaName, latitude, longitude, source, sourceUrl, dataOrigin, timeZone, fetchedAt` |
| `hourly` | 168 mốc gồm `validAt, temperatureC, relativeHumidityPercent, precipitationMm, precipitationProbabilityPercent, windSpeedKmh, weatherCode` |
| `daily` | 7 ngày gồm `date, minTemperatureC, maxTemperatureC, precipitationMm, maxPrecipitationProbabilityPercent, weatherCode` |

- Chuỗi `hourly` bắt đầu từ **00:00 của hôm nay tại Việt Nam**, gồm cả các giờ đã qua trong ngày. `validAt` là ISO 8601 có offset `+07:00`. Nếu giao diện chỉ hiển thị các giờ sắp tới, lọc theo thời điểm hiện tại.
- `daily.date` là ngày địa phương theo định dạng `YYYY-MM-DD`. Hiển thị như ngày lịch và giữ nguyên ngày khi đổi múi giờ thiết bị. `fetchedAt` là thời điểm backend lấy dữ liệu.
- `precipitationMm` trong chuỗi giờ là lượng mưa trong giờ trước mốc `validAt`. Trong chuỗi ngày, đó là tổng lượng mưa của ngày. Các biến thời tiết có thể là `null` khi nhà cung cấp thiếu dữ liệu. Giữ `0` là giá trị riêng. [Tài liệu biến dự báo Open-Meteo](https://open-meteo.com/en/docs#api-documentation)

Ví dụ một phần tử của `hourly`.

```json
{
  "validAt": "2026-11-26T00:00:00+07:00",
  "temperatureC": 20.1,
  "relativeHumidityPercent": 82,
  "precipitationMm": 0,
  "precipitationProbabilityPercent": null,
  "windSpeedKmh": 6.2,
  "weatherCode": 2
}
```

## Danh mục, quan trắc và dự báo

Tất cả endpoint trong mục này công khai.

| Endpoint | Kết quả |
| --- | --- |
| `GET /basins` | `200` mảng lưu vực hoạt động gồm `id, code, name, description`. |
| `GET /basins/{id}/stations` | `200` mảng trạm gồm `id, code, name, type, latitude, longitude`. Tọa độ có thể là `null`. |
| `GET /stations/{id}/metrics` | `200` mảng `code, name, quantityType, unit`. Đơn vị của bộ demo có thể là `null`. |
| `GET /stations/{id}/observations` | `200` trang quan trắc. Gửi `metric, from, to` và tùy chọn `page, pageSize`. |
| `GET /stations/{id}/models` | `200` mảng `version, horizonHours` của mô hình đang bật. Mảng rỗng nếu trạm chưa có mô hình hoạt động. |
| `POST /forecasts/historical` | `200` lượt dự báo mới. Gửi `stationId, issueTimeLocal`. |

Ví dụ quan trắc: `GET /api/v1/stations/1/observations?metric=water_level&from=2021-01-01&to=2021-01-02&pageSize=1`.

- `from` bao gồm, `to` loại trừ. Dùng ngày ISO hoặc giờ ISO theo dữ liệu nguồn. Các mốc như `2021-01-01T08:00:00` đi dưới dạng chuỗi, giữ nguyên giá trị đồng hồ và bỏ qua chuyển đổi múi giờ của trình duyệt.
- Mỗi phần tử quan trắc gồm `timestampLocal, valueRaw, qualityStatus, availableAt, source`. `source` gồm `id, name, url`. `availableAt` và `source.url` có thể là `null`. Quan trắc demo có `qualityStatus = SOURCE_UNVERIFIED`. Ví dụ điểm đầu của mực nước Trung Trang có `timestampLocal = 2021-01-01T00:00:00` và `valueRaw = -7`. Giữ dấu và giá trị gốc để vẽ.
- Nút dự báo dùng mốc đầu giờ giữa 2021 và 2022 theo dữ liệu nguồn, định dạng chính xác `YYYY-MM-DDTHH:mm:ss` với phút và giây đều bằng 0. Gửi chuỗi như `2022-10-15T12:00:00`, không gắn `Z` hoặc offset. Backend cần 48 mốc liên tiếp cho mực nước và lưu lượng Trung Trang cùng mực nước Hòn Dấu.
- Phản hồi dự báo gồm `runId, stationId, issueTimeLocal, timeBasis, modelVersion, modelBundleHash, inputHash, sourceName, dataQualityStatus, unitStatus, thresholdType, thresholdSource, thresholdValueRaw, predictions`. Mỗi phần tử `predictions` gồm `horizonHours, targetTimeLocal, predictedWaterLevelRaw, observedWaterLevelRaw, warningStatus`. Thực đo có thể là `null`. `warningStatus` có giá trị `NORMAL` hoặc `THRESHOLD_EXCEEDED` khi dự báo lớn hơn hoặc bằng Q95. Chú thích giao diện là **so với ngưỡng nghiên cứu**.
- API gọi worker cho từng request hợp lệ. Nếu worker không hoạt động, yêu cầu trả `503` kể cả với thời điểm từng chạy.

Ví dụ điểm dự báo +1 giờ trong mảng `predictions`.

```json
{
  "horizonHours": 1,
  "targetTimeLocal": "2022-10-15T13:00:00",
  "predictedWaterLevelRaw": 134.6441650390625,
  "observedWaterLevelRaw": 132,
  "warningStatus": "NORMAL"
}
```

## Xác thực và hồ sơ

| Quyền | Endpoint | Body và kết quả |
| --- | --- | --- |
| Công khai | `POST /auth/register` | `{ email, password, displayName }` → `201` hồ sơ. Tài khoản mới có vai trò `USER`. |
| Công khai | `POST /auth/login` | `{ email, password }` → `200` với `accessToken, tokenType, expiresAt, user`. |
| Đăng nhập | `POST /auth/logout` | Body rỗng → `204`. Thu hồi phiên hiện tại. |
| Đăng nhập | `GET /me` | `200` hồ sơ hiện tại. |
| Đăng nhập | `PATCH /me` | `{ displayName }` → `200` hồ sơ đã cập nhật. |
| Đăng nhập | `POST /auth/change-password` | `{ currentPassword, newPassword }` → `204`. Đăng nhập lại sau khi đổi. |
| Công khai | `POST /auth/forgot-password` | `{ email }` → `202` khi dịch vụ gửi đã được cấu hình. Dùng chung thông báo cho mọi email. |
| Công khai | `POST /auth/reset-password` | `{ token, newPassword }` → `204`. Mã gửi qua email, dùng một lần trong 30 phút. |
| ADMIN | `PATCH /admin/users/{id}/status` | `{ locked: true }` để khóa `USER`, `false` để mở khóa. Trả `200` hồ sơ. |

Hồ sơ gồm `id, email, displayName, role, status, createdAt`. Vai trò gồm `USER, ADMIN`, trạng thái gồm `ACTIVE, LOCKED`. Mật khẩu mới dài 12–128 ký tự. `displayName` tối đa 100 ký tự. Đổi hoặc đặt lại mật khẩu thu hồi các phiên. API khóa người dùng cần ID mà Admin đã biết.

Ví dụ đăng nhập bằng tài khoản thử đã được đăng ký, sau đó lấy hồ sơ bằng `accessToken` nhận được.

```http
POST /api/v1/auth/login
Content-Type: application/json

{"email":"frontend@example.test","password":"ReplaceWithDemoPassword"}
```

```http
GET /api/v1/me
Authorization: Bearer <accessToken>
```

## Cộng đồng

| Quyền | Endpoint | Body và kết quả |
| --- | --- | --- |
| Công khai | `GET /areas` | `200` mảng khu vực hoạt động gồm `id, parentId, code, name, level, weatherLatitude, weatherLongitude`. Tùy chọn `search` và `hasWeatherLocation`. |
| Công khai | `GET /areas/{id}/posts` | `200` bảng tin theo khu vực và khu vực con hoạt động. Lọc theo `type, from, to, page, pageSize`. |
| Công khai | `GET /posts/{id}` | `200` chi tiết bài đã xuất bản. |
| Công khai | `GET /posts/{id}/comments` | `200` danh sách bình luận công khai có phân trang. |
| Đăng nhập | `GET /me/posts`, `GET /me/posts/{id}` | `200` danh sách hoặc chi tiết bài của mình. |
| Đăng nhập | `GET /me/comments`, `GET /me/comments/{id}` | `200` danh sách hoặc chi tiết bình luận của mình. |
| Đăng nhập | `POST /posts` | `{ areaId, stationId?, title, content, postType, observedAt }` → `201` bài chờ duyệt. |
| Tác giả | `PATCH /posts/{id}` | Gửi các trường cần sửa → `200` bài chờ duyệt. `stationId: null` bỏ liên kết trạm. |
| Tác giả | `DELETE /posts/{id}` | Body rỗng → `204`, xóa mềm. |
| Đăng nhập | `POST /posts/{id}/comments` | `{ content }` → `201` bình luận hiển thị ngay trên bài công khai. |
| Tác giả | `PATCH /comments/{id}`, `DELETE /comments/{id}` | PATCH nhận `{ content }`, DELETE body rỗng. Cả hai trả `204`. |
| Tác giả | `POST /posts/{postId}/media` | Multipart trường `file` → `201` với `id, mediaType, sortOrder`. |
| Theo quyền xem bài | `GET /posts/{postId}/media/{mediaId}` | `200` dữ liệu ảnh. Với bài chưa công khai, tác giả hoặc ADMIN gửi Bearer token. |
| Tác giả | `DELETE /posts/{postId}/media/{mediaId}` | Body rỗng → `204`. |
| Đăng nhập | `POST /content-reports` | `{ postId, reason }` hoặc `{ commentId, reason }` → `201` với `id, status`. |

- `GET /api/v1/areas?search=hai%20phong&hasWeatherLocation=true` tìm khu vực có tọa độ Weather. `search` tìm theo tên hoặc mã, tối đa 100 ký tự, hỗ trợ có dấu hoặc không dấu. Với danh sách khu vực cộng đồng, bỏ bộ lọc tọa độ. `weatherLatitude` và `weatherLongitude` cùng là `null` khi chưa cấu hình.
- `postType` và bộ lọc `type` nhận `FLOOD_REPORT, WEATHER_UPDATE, DISCUSSION, OTHER`. Tiêu đề tối đa 160 ký tự, nội dung bài 5.000, bình luận 2.000, lý do báo cáo 1.000.
- `observedAt` và `from/to` của **cộng đồng** dùng ISO có offset, ví dụ `2022-10-15T12:00:00+07:00`. Khi đưa offset vào query string, dùng `URLSearchParams` để mã hóa dấu `+`. Bộ lọc bảng tin chạy trên `publishedAt`, bao gồm `from` và loại trừ `to`.
- Bài mới hoặc sửa có trạng thái `PENDING`. Danh sách công khai chỉ thấy nội dung `APPROVED` đã xuất bản. Tác giả đọc trạng thái, `moderationReason` và `rejectionReason` qua `/me`. Bình luận thông thường hiển thị ngay và sửa bình luận đang công khai vẫn hiển thị. Sửa bình luận `HIDDEN` hoặc `REJECTED` chuyển về `PENDING`.
- Đối tượng bài gồm `id, areaId, stationId, author, title, content, postType, observedAt, moderationStatus, publishedAt, moderationReason, rejectionReason, media`. Bình luận gồm `id, postId, author, content, moderationStatus, createdAt, publishedAt, moderationReason, rejectionReason`. `author` gồm `id, displayName`.
- Tối đa 5 ảnh mỗi bài, 5 MiB mỗi ảnh, định dạng JPEG, PNG hoặc WebP. Dùng `FormData` và để trình duyệt đặt header multipart. Với ảnh cần Bearer token, dùng `fetch` lấy blob để hiển thị.

## Theo dõi cá nhân

Tất cả endpoint cần đăng nhập. PUT và DELETE trả `204` với body rỗng. GET trả `200`.

| Endpoint | Chức năng |
| --- | --- |
| `GET /me/followed-areas` | Mảng khu vực theo dõi đang hoạt động. |
| `PUT /me/followed-areas/{areaId}`, `DELETE /me/followed-areas/{areaId}` | Theo dõi hoặc bỏ theo dõi khu vực. |
| `GET /me/followed-areas/posts` | Trang bài đã xuất bản ở các khu vực theo dõi và khu vực con. Nhận `page, pageSize`. |
| `GET /me/followed-stations` | Mảng trạm theo dõi thuộc lưu vực đang hoạt động. |
| `PUT /me/followed-stations/{stationId}`, `DELETE /me/followed-stations/{stationId}` | Theo dõi hoặc bỏ theo dõi trạm. |

Mỗi mục trong danh sách theo dõi có `followedAt`. Gửi lại PUT giữ thời điểm theo dõi ban đầu. PUT trả `404` nếu đối tượng không khả dụng.

## Kiểm duyệt

Tất cả endpoint cần ADMIN.

| Endpoint | Body và kết quả |
| --- | --- |
| `GET /admin/moderation/queue` | `200` danh sách chờ, lọc `kind=post` (mặc định) hoặc `kind=comment`. |
| `POST /admin/posts/{id}/{decision}` | `204` sau khi xử lý. `decision` gồm `approve, reject, hide`. |
| `POST /admin/comments/{id}/{decision}` | `204` sau khi xử lý. Cùng quy tắc như quyết định cho bài. |
| `GET /admin/content-reports` | `200` danh sách báo cáo chờ, có `pendingReporterCount` để hiển thị số người báo cáo khác nhau. |
| `POST /admin/content-reports/{id}/resolve` | `{ resolution: "RESOLVED" }` hoặc `"DISMISSED"` → `204`. |
| `GET /admin/moderation/history` | `200` trang nhật ký có `adminId, postId, commentId, action, reason, createdAt`. |

`reject` và `hide` cần body `{ reason }`, `approve` có thể gửi body rỗng. Các danh sách nhận `page, pageSize`. Báo cáo và nhật ký nhận tối đa một bộ lọc trong `postId, commentId`. Việc gửi hoặc xử lý báo cáo không tự ẩn nội dung. Admin dùng quyết định `hide` khi cần.

## Quản trị danh mục

Tất cả endpoint cần ADMIN. Với mỗi nhóm dưới đây, dùng `GET /admin/catalog/{collection}` để liệt kê (`200`), `POST /admin/catalog/{collection}` để tạo (`201`) và `PUT /admin/catalog/{collection}/{id}` để cập nhật đầy đủ (`200`). Gửi các trường trạng thái tường minh khi sửa.

| Collection | JSON cho POST và PUT |
| --- | --- |
| `basins` | `code, name, description?, isActive` |
| `areas` | `code, name, level, parentId?, isActive` |
| `stations` | `code, name, type, basinId, areaId, latitude?, longitude?` |
| `sources` | `name, sourceType, url?, isActive`. Phản hồi trả `status` là `ACTIVE` hoặc `INACTIVE`. |
| `metrics` | `code, name, quantityType` |

Mã danh mục, lưu vực của trạm, tên và loại nguồn, loại đại lượng của metric là các khóa ổn định khi sửa. Trường có dấu `?` là tùy chọn. Trạng thái lưu vực, khu vực và nguồn có thể được cập nhật qua `isActive`.

| Endpoint | Body và kết quả |
| --- | --- |
| `PUT /admin/catalog/areas/{id}/weather-location` | `{ latitude, longitude }` → `200` khu vực. Gửi đủ hai trường, theo phạm vi −90…90 và −180…180, hoặc đặt cả hai thành `null` để xóa tọa độ. |
| `GET /admin/catalog/observations` | `200` trang quan trắc, có bộ lọc `stationId, qualityStatus, page, pageSize`. |
| `PUT /admin/catalog/observations/{id}/metadata` | `{ qualityStatus, availableAt }` → `200`. Dùng ISO có offset hoặc `null` để xóa `availableAt`. Các trạng thái gồm `SOURCE_UNVERIFIED, VALID, SUSPECT, INVALID`. |
| `GET /admin/catalog/actions` | `200` trang nhật ký gồm `id, adminId, action, resourceType, resourceKey, createdAt`. Lọc `resourceType, page, pageSize`. |
| `GET /admin/stations/{stationId}/models` | `200` mảng `id, version, horizonHours, artifactPath, manifestHash, isActive`. |
| `POST /admin/stations/{stationId}/models/register-van-uc` | Body rỗng → `204`. Cần dữ liệu Văn Úc và worker đang chạy. |
| `PATCH /admin/stations/{stationId}/models/{version}/status` | `{ isActive: true/false }` → `204`. Đổi trạng thái tất cả kỳ trong phiên bản. |
| `GET /admin/stations/{stationId}/thresholds` | `200` mảng `id, thresholdType, valueRaw, source, unitStatus, isActive`. |
| `PATCH /admin/stations/{stationId}/thresholds/{thresholdId}/status` | `{ isActive: true/false }` → `204`. |

API metadata giữ nguyên giá trị và giờ đo gốc. Dự báo bỏ qua bản ghi `SUSPECT` và `INVALID` khi lấy đầu vào và thực đo đối chiếu. Góc nhìn Admin về mô hình gồm cả phiên bản đã tắt.
