# Kiểm tra phần Nguyên

Kết quả xác minh ngày 12/11/2026: biên dịch thành công, không cảnh báo/lỗi; bộ kiểm tra nghiệp vụ đạt; 36 kiểm tra HTTP đạt, gồm gửi form xác nhận đơn → Admin hoàn tất → xuất hóa đơn → lập phiếu → khách tra qua API. Đã kiểm tra bố cục ở chiều rộng 1440px và 390px. Các thao tác ghi chạy trên bản sao riêng; đối chiếu mã băm xác nhận 12 file XML gốc không đổi.

Chạy từ thư mục gốc repository khi đã khôi phục các gói .NET:

```powershell
dotnet run --project Tests/DonHangSmoke -p:UseAppHost=false -p:NuGetAudit=false -- AurelMaison
```

Bộ kiểm tra sao chép XML/XSD/XSLT vào thư mục riêng dưới thư mục chạy, không sửa dữ liệu đang vận hành. Kiểm tra tạo đơn, dữ liệu sai, quyền đọc/hủy, xác nhận toàn bộ serial hoặc từ chối cả danh sách, hai yêu cầu cùng giữ một serial, giải phóng kho, giao hàng, hoàn tất, doanh số đúng nhân viên, hóa đơn XSLT, bán tại quầy, bảo hành, phản hồi XML, xử lý lịch hẹn và tính hợp lệ của cả 12 file sau các thao tác.

Controller phía khách phải lấy `maKH` từ `User.MaKH()`, không từ form. Kết quả tạo đơn chứa `Ma`, `TongTien`, `ThanhCong`, `ThongBao`; chỉ xóa giỏ khi thành công. Tạo đơn online chưa giữ serial; bước xác nhận mới giữ. Tạo đơn tại quầy hoàn tất ngay.

Kiểm tra middleware xác thực và Razor trên website đang chạy:

```powershell
./Tests/StaffHttpSmoke.ps1 -BaseUrl http://localhost:5269
```

Bộ HTTP mặc định dùng tài khoản demo trong README; xác minh 401/403/404, phân quyền Staff/Admin, XML của đúng chủ phiếu, 10 trang hoạt động, chặn POST thiếu token và GET thay đổi đơn. Bộ kiểm tra dùng dữ liệu mẫu gốc; không chạy trên dữ liệu đã sửa nếu muốn so sánh đúng kết quả mẫu.

Tùy chọn `-MutateTestData` kiểm tra thêm gửi form xác nhận, hoàn tất và lập bảo hành cho đơn mẫu `AM20260018`. Tùy chọn này thay đổi dữ liệu: chỉ dùng khi website đã khởi động với `--contentRoot` trỏ tới bản sao riêng chứa `App_Data/Xml`, `App_Data/Xsd`, `App_Data/Xslt`. Không dùng với dữ liệu vận hành.

Sau khi tích hợp `main` ngày 13/11/2026, `CustomerOrderAdapter` đã nối khu khách với dịch vụ đơn hàng thật; lịch hẹn dùng chung dịch vụ của Hiệp. Chạy thêm `-IntegratedFlow -MutateTestData` trên website dùng bản sao dữ liệu để kiểm tra khách đặt hàng → Staff hoàn tất → khách tra bảo hành qua HttpClient và khách đặt lịch → Staff xác nhận → tài khoản khách thấy trạng thái mới. Đặt cấu hình `Warranty:BaseUrl` trỏ về chính địa chỉ website kiểm thử. Cả 65 kiểm tra HTTP đã đạt; kiểm tra nghiệp vụ đạt và build không có lỗi/cảnh báo. Chưa kiểm thử đồng bộ với SQL Server thật. Chưa có rollback sau mất điện giữa nhiều file, theo phạm vi chốt của nhóm.
