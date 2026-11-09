⌚ HỆ THỐNG QUẢN LÝ BÁN ĐỒNG HỒ (XML & .NET)
![XML](https://img.shields.io/badge/Data-XML%20%7C%20XSD%20%7C%20XSLT-orange)
![Platform](https://img.shields.io/badge/Platform-C%23%20WinForms%20%2F%20Web-blue)
![Service](https://img.shields.io/badge/Integration-RSS%20%7C%20Web%20Service-success)
![Status](https://img.shields.io/badge/Status-In%20Progress-yellow)
> **Đồ án môn học:** Công nghệ XML và Ứng dụng  
> **Đề tài:** Mô phỏng hệ thống Quản lý bán đồng hồ bằng Windows Forms / Web kết hợp XML; Mô tả ứng dụng Web Service hoặc RSS.  
> **Repository:** `https://github.com/nguyenhuuhiep386-dotcom/CN_XML_QUANLYBANDONGHO.git`
---
👥 Thành viên thực hiện & Phân công nhiệm vụ
STT	Mã Sinh Viên	Họ và Tên	Vai trò	Nhiệm vụ chi tiết
1	2415053122218	Nguyễn Hữu Hiệp	Nghiệp vụ Bán hàng & Khách hàng	• Thiết kế `KhachHang.xml`, `HoaDon.xml`, `ChiTietHoaDon.xml`.<br>• Xây dựng chức năng Quản lý Khách hàng (Thêm, sửa, xóa, tìm kiếm).<br>• Xây dựng nghiệp vụ Lập hóa đơn bán hàng, tự động kiểm tra và trừ tồn kho trong `DongHo.xml`.<br>• Tra cứu lịch sử hóa đơn theo ngày/khách hàng.
2	2415053122224	Đặng Thái Nguyên	Kiến trúc XML, Sản phẩm & RSS / Web Service	• Thiết kế `DongHo.xml`, `HangSanXuat.xml`, `TaiKhoan.xml` và ràng buộc `XSD`.<br>• Xây dựng chức năng Đăng nhập, Quản lý Đồng hồ & Hãng sản xuất bằng `LINQ to XML` / `XPath`.<br>• Xây dựng module đọc RSS Feed (Tin tức đồng hồ / Tỷ giá ngoại tệ XML) hoặc Web Service tra cứu bảo hành.
3	2415053122230	Võ Văn Phương	Báo cáo XSLT, Đồng bộ SQL & Thuyết trình	• Thiết kế `HoaDon.xslt`, `BaoCaoDoanhThu.xslt` xuất hóa đơn và thống kê ra giao diện Web (`HTML`).<br>• Xây dựng chức năng Thống kê doanh thu, sản phẩm bán chạy.<br>• Chuyển đổi dữ liệu giữa SQL Server và XML (Sao lưu / Phục hồi).<br>• Tổng hợp báo cáo Word và chuẩn bị Slide thuyết trình.
---
🚀 Các chức năng chính của hệ thống
Quản lý Danh mục & Hệ thống (XML DOM / LINQ to XML / XPath):
Đăng nhập và phân quyền người dùng (`TaiKhoan.xml`).
Quản lý thông tin đồng hồ: Mã đồng hồ, tên, thương hiệu, loại máy (Automatic, Quartz, Smartwatch), đơn giá, số lượng tồn, thời gian bảo hành (`DongHo.xml`).
Quản lý hãng sản xuất / nhà cung cấp (`HangSanXuat.xml`).
Quản lý Nghiệp vụ Bán hàng:
Quản lý hồ sơ khách hàng (`KhachHang.xml`).
Lập hóa đơn bán hàng, thêm giỏ hàng, tính tổng tiền tự động và cập nhật số lượng tồn kho (`HoaDon.xml`, `ChiTietHoaDon.xml`).
Biểu diễn dữ liệu & Thống kê (XSLT -> HTML):
Biến đổi dữ liệu hóa đơn từ XML sang định dạng HTML bằng `XSLT` để in phiếu thanh toán cho khách hàng.
Xuất báo cáo thống kê doanh thu và danh sách mặt hàng bán chạy ra trình duyệt.
Ứng dụng RSS / Web Service & Đồng bộ dữ liệu:
Tích hợp RSS / XML trực tuyến: Đọc luồng tin tức thị trường đồng hồ qua chuẩn RSS 2.0 và cập nhật tỷ giá ngoại tệ trực tiếp từ XML ngân hàng để quy đổi giá đồng hồ quốc tế.
Sao lưu & Phục hồi: Chuyển đổi qua lại giữa Cơ sở dữ liệu SQL Server và các tập tin XML.
---
📂 Cấu trúc thư mục dự kiến
```text
CN_XML_QUANLYBANDONGHO/
├── 📁 src/                         # Mã nguồn chương trình (C# WinForms / Web)
│   ├── 📁 Forms/                   # Giao diện người dùng (UI)
│   ├── 📁 Services/                # Các lớp xử lý XML, XPath, XSLT, RSS, SQL
│   └── 📁 Models/                  # Các lớp thực thể (Entity)
├── 📁 Data_XML/                    # Chứa các file dữ liệu gốc của hệ thống
│   ├── 📄 TaiKhoan.xml
│   ├── 📄 HangSanXuat.xml
│   ├── 📄 DongHo.xml
│   ├── 📄 KhachHang.xml
│   ├── 📄 HoaDon.xml
│   └── 📄 ChiTietHoaDon.xml
├── 📁 Schemas_XSD/                 # Chứa các file kiểm tra ràng buộc dữ liệu (.xsd)
├── 📁 Templates_XSLT/              # Chứa các file định dạng xuất báo cáo/hóa đơn (.xslt)
│   ├── 📄 PhieuHoaDon.xslt
│   └── 📄 BaoCaoDoanhThu.xslt
├── 📁 Docs/                        # Tài liệu báo cáo Word, Slide thuyết trình, Database (.sql)
└── 📄 README.md                    # Tài liệu giới thiệu dự án
```
---
🛠️ Công nghệ sử dụng
Ngôn ngữ lập trình: C# (.NET Framework / .NET Core)
Công nghệ XML: XML 1.0, XSD (XML Schema), XPath, XSLT, LINQ to XML, `XmlDocument`
Tích hợp mở rộng: RSS 2.0 (`SyndicationFeed` / `XDocument`), Web Service (ASMX / Web API)
Công cụ phát triển: Visual Studio 2022, Visual Studio Code, SQL Server
---
💻 Hướng dẫn cài đặt và Làm việc nhóm với Git
1. Clone dự án về máy (Dành cho các thành viên trong nhóm)
Mở Terminal / Git Bash và chạy lệnh:
```bash
git clone https://github.com/nguyenhuuhiep386-dotcom/CN_XML_QUANLYBANDONGHO.git
cd CN_XML_QUANLYBANDONGHO
```
2. Quy trình đẩy code lên GitHub tránh xung đột (Conflict)
Mỗi thành viên nên tạo nhánh (branch) riêng hoặc kéo code mới nhất về trước khi push:
```bash
# Bước 1: Kéo code mới nhất từ trên mạng về máy trước khi làm việc
git pull origin main

# Bước 2: Thêm các file vừa code xong
git add .

# Bước 3: Lưu lại lịch sử thay đổi (ghi rõ tên người làm và chức năng)
git commit -m "Nguyen: Them chuc nang CRUD DongHo.xml va doc RSS"

# Bước 4: Đẩy code lên GitHub
git push origin main
```
---
© 2026 - Nhóm Đồ Án Công Nghệ XML (Hiệp - Nguyên - Phương)
