# FloodAI Backend

## Khôi phục ngữ cảnh

- Repo Git backend nằm tại `D:\Repo\flood ai\backend`. Chạy lệnh Git và .NET từ thư mục này.
- Khi bắt đầu một task mới hoặc tiếp tục sau compact, đọc PROGRESS.md ở thư mục repo nếu tệp có trên máy. Đối chiếu với yêu cầu mới nhất của người dùng, `git status --short --branch`, `git log -3 --oneline` và các tệp liên quan trước khi sửa. Giữ nguyên các thay đổi đang có và tiếp tục từ điểm dừng đã xác minh.
- Cập nhật `PROGRESS.md` ngay sau mỗi mốc có thay đổi đáng kể, kể cả giữa một task dài. Ghi việc đã làm, kết quả kiểm tra thực tế, phần còn dở, quyết định của người dùng và bước kế tiếp. Giữ bản ghi ngắn, không lưu thông tin xác thực hoặc sao chép toàn bộ cuộc trò chuyện.
- Khi hoàn thành bước người dùng giao, báo kết quả và hỏi họ có muốn bắt đầu bước kế tiếp hay không. Chờ câu trả lời trước khi mở rộng công việc sang bước đó.

## Điểm vào kỹ thuật

- [README.md](README.md) có lệnh chạy backend .NET 10, cấu hình PostgreSQL và hướng dẫn EF Core.
- Đặc tả nằm trong `../FloodAI_Functional_Database_Design_v1.0.docx`. Dữ liệu và mã ML nằm trong `../datasets` và `../ml`.
