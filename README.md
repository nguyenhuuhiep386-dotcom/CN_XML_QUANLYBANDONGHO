# ⌚ HỆ THỐNG QUẢN LÝ BÁN ĐỒNG HỒ (XML & .NET)

> **Đồ án môn học:** Công nghệ XML và Ứng dụng  
> **Đề tài:** Xây dựng website quản lý bán đồng hồ bằng C#/.NET kết hợp XML; ứng dụng XSD, XPath, XSLT và RSS hoặc Web Service.  
> **Repository:** [CN_XML_QUANLYBANDONGHO](https://github.com/nguyenhuuhiep386-dotcom/CN_XML_QUANLYBANDONGHO)

## 📖 Giới thiệu

Hệ thống hỗ trợ khách hàng tìm kiếm, đặt mua đồng hồ trực tuyến; hỗ trợ nhân viên xử lý bán hàng tại cửa hàng và trên website; hỗ trợ Admin quản lý sản phẩm, kho hàng, tài khoản và báo cáo.

Dữ liệu nghiệp vụ được tổ chức bằng XML. Hệ thống phân biệt **mẫu đồng hồ** và **từng chiếc đồng hồ theo serial** để theo dõi tồn kho, giao hàng và bảo hành.

**Phạm vi tài liệu:** README mô tả yêu cầu và thiết kế dự kiến của đồ án. Các chức năng dưới đây không phải danh sách xác nhận đã triển khai. Khi có khác biệt, mục **Phạm vi chốt** ngay bên dưới được ưu tiên hơn các mục còn lại, kể cả bảng phân công cũ.

## 📌 Phạm vi chốt

Nhóm làm theo 32 màn hình trên Figma (gồm 2 màn hình mới: Dữ liệu XML, Tài khoản của tôi), cộng các mục XML bắt buộc. Công nghệ: ASP.NET Core MVC (.NET 8), LINQ to XML, SQL Server. Lúc chạy, website đọc/ghi file XML; XML là dữ liệu đang vận hành. SQL Server chỉ dùng để khởi tạo (SQL → XML một lần trước khi chạy) và nhận bản đồng bộ (XML → SQL). Không chạy SQL → XML đè lên dữ liệu đang chạy, trừ khi phục hồi có chủ đích.

**Bắt buộc có:**

- 12 bảng SQL ↔ 12 file XML, mỗi file có một XSD và được kiểm tra hợp lệ trước khi ghi.
- Thêm / sửa / xóa / tìm kiếm trên XML.
- 3 file XSLT xuất HTML: `DanhMucDongHo`, `PhieuHoaDon`, `BaoCaoDoanhThu`.
- Chuyển SQL → XML và XML → SQL, sao lưu và phục hồi.
- Web Service tra cứu bảo hành trả về XML.
- 3 vai trò Khách / Nhân viên / Admin, kiểm tra quyền ở máy chủ.

**Đưa vào "Hướng phát triển" (không làm):** giỏ hàng lưu theo tài khoản, giá riêng theo từng chiếc, điều chỉnh kho, nhật ký kho, chống gửi lặp, giao dịch nhiều file có rollback, hai nhân viên tranh nhận một việc, liên kết hồ sơ khách tại quầy với tài khoản, RSS/tỷ giá/khuyến mãi.

### Quy ước

Mọi người đọc/ghi XML chỉ qua `XmlHelper` và dịch vụ của người sở hữu file, không tự mở file XML của người khác để ghi.

File XML có gốc `DanhSach<Bảng>`, mỗi dòng là một phần tử `<Bảng>`, mỗi cột là một phần tử con cùng tên cột SQL. XML và XSD cùng tên, ví dụ `Xml/DongHo.xml` và `Xsd/DongHo.xsd`.

| Đối tượng | Mẫu mã | Ví dụ |
| --- | --- | --- |
| Tài khoản / Khách / Nhân viên | TK / KH / NV + 3 số | TK001, KH001, NV001 |
| Hãng / Mẫu đồng hồ | HG / DH + 3 số | HG001, DH001 |
| Serial | Chuỗi do Admin nhập, duy nhất | DEMO001 |
| Phiếu nhập | PN + 3 số | PN001 |
| Đơn hàng | AM + năm + 4 số | AM20260018 |
| Hóa đơn / Lịch hẹn / Phiếu bảo hành | HD / LH / BH + năm + 4 số | BH20260012 |

Mã trạng thái nằm trong `Models/HangSo.cs`, lưu không dấu, giao diện hiển thị tiếng Việt. Tồn khả dụng = số chiếc `ConHang`, không lưu cột số lượng riêng.

Kiểu dữ liệu: tiền là decimal (VNĐ, không số lẻ); ngày giờ ISO `2026-11-09T14:30:00`; ngày `2026-11-09`; có/không là `true`/`false`. Mật khẩu băm và kiểm tra bằng `PasswordHasher<TaiKhoan>` của ASP.NET Core, lưu nguyên chuỗi kết quả vào cột `MatKhauHash`.

Mỗi chi tiết đơn = một chiếc. Khách mua 2 chiếc cùng mẫu thì đơn có 2 dòng chi tiết; serial để trống lúc đặt, nhân viên gán khi xác nhận.

### Dùng XmlHelper và viết XSD

Mọi thao tác đọc → kiểm tra → sửa → ghi nằm trong một khối khóa; hàm ghi trả `KetQua`, không ném lỗi ra giao diện:

```csharp
var kq = XmlHelper.ChayTrongKhoa(() =>
{
    var ma = XmlHelper.SinhMa("KH", "KhachHang", "MaKH");       // SinhMa phải cùng khối với lần ghi
    return XmlHelper.Them("KhachHang", "MaKH", new XElement("KhachHang",
        new XElement("MaKH", ma),
        new XElement("HoTen", hoTen),
        new XElement("NgayTao", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"))));
});
```

- Hàm dùng chung: `Load`, `Save`, `KiemTraXsd`, `TimTheoMa`, `Them`, `Sua`, `Xoa`, `SinhMa`, `XuatHtml`. Khóa ghép như `ChiTietDonHang` (`MaDon` + `STT`) thì dùng `Load` và `Save`.
- Thao tác ghi hai file: gọi `KiemTraXsd` cho cả hai trước khi `Save` file đầu tiên.
- XSD làm theo [HangSanXuat.xsd](AurelMaison/App_Data/Xsd/HangSanXuat.xsd): dòng dùng `xs:all` (không dùng `xs:sequence`, vì cột NULL gán sau sẽ nằm cuối dòng), cột NULL có `minOccurs="0"`, khóa chính dùng `xs:key`.

### Đăng nhập và lấy người dùng hiện tại

Tài khoản demo, mật khẩu `123456`: `admin@aurel.vn` (Admin → `/Admin`), `nhanvien@aurel.vn` (NhanVien → `/Staff`), `khach1@gmail.com`, `khach2@gmail.com` (KhachHang → trang chủ).

Mã người đang đăng nhập luôn lấy từ `User`, không nhận từ form: `User.MaKH()`, `User.MaNV()`, `User.MaTK()`, `User.VaiTro()` (cần `using AurelMaison.Services;`). Khu nhân viên dùng `[Authorize(Roles = VaiTro.NhanVienHoacAdmin)]`, khu Admin dùng `[Authorize(Roles = VaiTro.Admin)]`. Đường dẫn `/api/*` trả 401 khi chưa đăng nhập, 403 khi sai vai trò. Tài khoản bị khóa hoặc đổi vai trò bị đăng xuất ngay ở yêu cầu kế tiếp.

### Dịch vụ dùng chung

Nhận qua constructor (đã đăng ký trong `Program.cs`):

- `DanhMucService`: `LayDanhSach(tuKhoa, maHang, giaTu, giaDen, loaiMay, chatLieu)` (chỉ mẫu `DangBan`, có `SoConHang`), `LayChiTiet(maMau)`, `LayDanhSachHang()`.
- `KhoService`: `DemConHang(maMau)`, `LaySerialConHang(maMau)`, `GiuSerial(ds)`, `GiaiPhongSerial(ds)`, `DanhDauDaBan(ds)`. Một serial sai thì cả danh sách không đổi; gọi bên trong `XmlHelper.ChayTrongKhoa` của đơn vẫn an toàn.
- `KhachHangService`: `TimTheoSoDienThoai(sdt)`, `ThemKhachTaiQuay(hoTen, sdt, email, diaChi)` (trùng số thì `Ma` là mã khách đã có), `CapNhatHoSo(maKH, ...)`, `LayTheoTaiKhoan(maTK)`.

### Dữ liệu XML và đồng bộ SQL

Màn `/Admin/DuLieu` (chỉ Admin): kiểm tra toàn bộ 12 file (XSD, trùng khóa, tham chiếu), thử XSD một file, XML → SQL (một transaction, lỗi thì rollback), SQL → XML ra `App_Data/KiemTra/<thời điểm>` kèm bảng đối chiếu, Khởi tạo từ SQL (ghi đè `App_Data/Xml`, phải tick xác nhận), sao lưu / phục hồi `App_Data/Backup`. Mọi thao tác ghi đều tự sao lưu trước. Chuỗi kết nối ở `ConnectionStrings:QuanLyBanDongHo` trong `appsettings.json`.

Đã có đủ XSD cho 12 bảng, gồm LichHen, DonHang, ChiTietDonHang, HoaDon và PhieuBaoHanh. Màn Dữ liệu XML dùng các lược đồ này để kiểm tra dữ liệu.

Chuẩn bị demo (`AurelMaison/Database/Demo/`):

- Bước 0: chạy `Buoc0_XoaDemo001_002.sql` trong SSMS, rồi trên web bấm "Khởi tạo từ SQL" → DH001 còn 0 chiếc. Bước 1: Admin nhập DEMO001, DEMO002 ở Serial & nhập kho.
- Bước 9: `GiaSai.xml` (có `<Gia>abc</Gia>`) → Dữ liệu XML → Thử XSD một file, bảng DongHo → bị từ chối ở dòng 8.

### Chạy dự án

Cần .NET SDK 8 trở lên; máy chỉ có .NET 10 vẫn build và chạy được.

```bash
dotnet run --project AurelMaison -p:UseAppHost=false --launch-profile http
```

Website chạy ở `http://localhost:5269`. Chuỗi kết nối nằm trong `appsettings.json`, mặc định là instance `.` với Windows Authentication; đổi thành `.\SQLEXPRESS` nếu máy dùng bản đó.

### Phần Nguyên đã triển khai

- `/Staff`: tổng quan đơn chờ, lịch hôm nay, doanh số tháng theo nhân viên phụ trách.
- `/Staff/DonHang`: tìm/lọc đơn; chi tiết, xác nhận và giữ serial, giao hàng, hoàn tất/lập hóa đơn, hủy và giải phóng serial.
- `/Staff/DonHang/TaiQuay`: tìm/thêm khách, chọn mẫu và serial, tính tổng tạm tính; máy chủ tính lại giá và hoàn tất đơn khi thanh toán.
- `/Staff/HoaDon/{maHD}`: hóa đơn HTML qua `PhieuHoaDon.xslt`, có nút In.
- `/Staff/LichHen`: lọc, xác nhận, hoàn tất, hủy có lý do. Dùng chung `LichHenService` với trang đặt lịch và tài khoản khách của Hiệp.
- `/Staff/BaoHanh`: lập phiếu cho serial thuộc đơn hoàn tất, chặn trùng, tính thời hạn từ dữ liệu chốt lúc bán.
- `GET /api/baohanh` và `GET /api/baohanh/{maPhieu}`: XML dành cho khách đang đăng nhập; 401 chưa đăng nhập, 403 sai vai trò/chủ phiếu, 404 không tìm thấy.

Admin dùng chung các trang Staff. Mọi thao tác thay đổi dùng POST và kiểm tra chống giả mạo yêu cầu. Mã nhân viên/khách lấy từ cookie, không nhận quyền từ form.

#### Bàn giao cho Hiệp

`DonHangService` đã đăng ký sẵn, nhận qua constructor. Ví dụ trong controller có `[Authorize(Roles = VaiTro.KhachHang)]`:

```csharp
var ketQua = donHangService.TaoDonOnline(User.MaKH() ?? "",
    new[] { new MucDatHang("DH001", 1) },
    new ThongTinNhanHang("Người nhận", "0901234567", "NhanTaiShowroom", Showroom: "Showroom Đà Nẵng"));
// Chỉ xóa giỏ khi ketQua.ThanhCong; lấy mã và tiền từ ketQua.Ma, ketQua.TongTien.
var danhSach = donHangService.LayTheoKhach(User.MaKH() ?? "");
var chiTiet = donHangService.LayChiTietTheoKhach(maDon, User.MaKH() ?? "");
var huy = donHangService.Huy(maDon, User.MaKH() ?? "", lyDo);
```

Các hàm đọc hiện trả `XElement`/`List<XElement>`; chi tiết chứa nút `ChiTiet` với các dòng `ChiTietDonHang`. `LayChiTiet` không giới hạn theo khách dành riêng cho controller Staff đã kiểm tra quyền. Hiệp dùng `LayChiTietTheoKhach` ở khu khách. Cột lưu phiếu là `MaPBH`, tên trường API là `MaPhieu` theo hợp đồng nhóm. API danh sách có gốc `DanhSachPhieuBaoHanh`.

Phần khách đã nối vào dịch vụ thật qua `CustomerOrderAdapter`: giỏ, thanh toán, theo dõi/hủy đơn và tra bảo hành qua HttpClient dùng chung dữ liệu với Staff. Không có SQL chạy trong các thao tác bán hàng. Khi nhiều file được ghi, kiểm tra đủ trước; có phục hồi lỗi ghi thông thường, chưa có giao dịch chống mất điện giữa các file.

#### Kiểm tra

```powershell
dotnet build AurelMaison -p:UseAppHost=false -p:NuGetAudit=false
dotnet run --project Tests/DonHangSmoke -p:UseAppHost=false -p:NuGetAudit=false -- AurelMaison
# Khi website đã chạy ở cổng 5269:
./Tests/StaffHttpSmoke.ps1 -BaseUrl http://localhost:5269
```

Bộ kiểm tra nghiệp vụ dùng bản sao riêng của dữ liệu và tự xóa sau khi chạy. Bộ HTTP mặc định chỉ đăng nhập, đọc trang và kiểm tra các yêu cầu bị từ chối, không thay đổi XML. Xem [hướng dẫn kiểm tra](Tests/DonHangSmoke/README.md).

## 👥 Thành viên thực hiện & Phân công nhiệm vụ

| STT | Mã sinh viên | Họ và tên | Vai trò | Nhiệm vụ chi tiết |
| --- | --- | --- | --- | --- |
| 1 | 2415053122218 | **Nguyễn Hữu Hiệp** | Bán hàng & Khách hàng | Thiết kế dữ liệu khách hàng, đơn hàng, chi tiết đơn hàng và hóa đơn.<br>Xây dựng hồ sơ khách hàng, địa chỉ nhận hàng, giỏ hàng, đặt hàng và bán trực tiếp.<br>Xử lý xác nhận, hủy, theo dõi đơn hàng; phối hợp kiểm tra tồn kho và gán serial.<br>Tra cứu lịch sử mua hàng và xây dựng chức năng đặt, xử lý lịch hẹn. |
| 2 | 2415053122224 | **Đặng Thái Nguyên** | Kiến trúc XML, Tài khoản, Sản phẩm & Kho | Thiết kế dữ liệu tài khoản, hãng sản xuất, mẫu đồng hồ, từng chiếc đồng hồ, nhập kho và XSD.<br>Xây dựng đăng ký, đăng nhập, phân quyền và quản lý tài khoản.<br>Quản lý thông số, hình ảnh, serial, giấy tờ, giá bán và tồn kho.<br>Xây dựng lớp xử lý XML dùng chung; tích hợp RSS hoặc Web Service. |
| 3 | 2415053122230 | **Võ Văn Phương** | Bảo hành, Báo cáo XSLT, SQL & Tài liệu | Thiết kế dữ liệu và chức năng lập, tra cứu phiếu bảo hành theo serial.<br>Thiết kế XSLT xuất hóa đơn và báo cáo HTML.<br>Thống kê doanh thu, tồn kho, sản phẩm bán chạy và doanh số nhân viên.<br>Chuyển đổi dữ liệu SQL Server ↔ XML phục vụ sao lưu, phục hồi.<br>Tổng hợp báo cáo Word và slide thuyết trình. |

Các thành viên thống nhất cấu trúc XML, mã định danh, quy tắc nghiệp vụ và giao diện các lớp dịch vụ trước khi tích hợp. Mỗi thành viên kiểm thử chức năng mình phụ trách và cùng kiểm thử quy trình bán hàng hoàn chỉnh.

## 🎭 Tác nhân & Chức năng hệ thống

### 1. Khách hàng

Khách hàng sử dụng website để tìm hiểu và mua đồng hồ:

- Đăng ký, đăng nhập, đăng xuất.
- Xem danh sách và chi tiết đồng hồ.
- Tìm kiếm, lọc theo thương hiệu, khoảng giá, loại máy và chất liệu.
- Quản lý giỏ hàng và đặt hàng.
- Xem lịch sử mua hàng, theo dõi đơn hàng của mình.
- Hủy đơn của mình khi đơn chưa được xác nhận.
- Đặt lịch tư vấn hoặc xem thử đồng hồ.
- Tra cứu bảo hành các đồng hồ đã mua.
- Cập nhật thông tin cá nhân và địa chỉ nhận hàng.

### 2. Nhân viên bán hàng

Nhân viên xử lý hoạt động bán hàng hằng ngày:

- Đăng nhập, đăng xuất, đổi mật khẩu và cập nhật thông tin cá nhân.
- Tra cứu đồng hồ, giá bán và tình trạng tồn kho.
- Xem, tiếp nhận, xác nhận và xử lý đơn hàng trong phạm vi được giao.
- Chọn chiếc đồng hồ theo serial để giao cho khách.
- Tạo đơn bán trực tiếp tại cửa hàng.
- Tra cứu, thêm và cập nhật thông tin khách hàng phục vụ bán hàng.
- Tạo lịch cho khách, xác nhận lịch tư vấn và lịch xem thử.
- Lập và tra cứu phiếu bảo hành.
- Xem doanh số các đơn do mình phụ trách.

**Giới hạn quyền:** Nhân viên không được quản lý tài khoản, phân quyền hoặc tự thay đổi giá bán. Việc cập nhật hồ sơ khách hàng không bao gồm thay đổi thông tin đăng nhập hay quyền tài khoản của khách.

### 3. Admin

Admin quản lý toàn bộ hệ thống:

- Đăng nhập, đăng xuất, đổi mật khẩu và cập nhật thông tin cá nhân.
- Quản lý tài khoản khách hàng và nhân viên: tạo, khóa, mở khóa.
- Phân quyền tài khoản.
- Quản lý thương hiệu, mẫu đồng hồ, thông số và hình ảnh.
- Quản lý từng chiếc đồng hồ: serial, tình trạng, giấy tờ và giá bán.
- Nhập kho và điều chỉnh tồn kho có ghi nhận lý do.
- Xem, tạo và xử lý toàn bộ đơn hàng, lịch hẹn, phiếu bảo hành.
- Quản lý chương trình khuyến mãi nếu triển khai.
- Xem thống kê doanh thu, tồn kho, sản phẩm bán chạy và doanh số nhân viên.
- Thực hiện sao lưu, phục hồi dữ liệu và chuyển đổi SQL Server ↔ XML.

## 🔐 Bảng phân quyền tổng quát

| Chức năng | Khách hàng | Nhân viên bán hàng | Admin |
| --- | --- | --- | --- |
| Xem, tìm kiếm đồng hồ | Có | Có | Có |
| Quản lý thông tin cá nhân | Của mình | Của mình | Của mình |
| Quản lý tài khoản, phân quyền | Không | Không | Có |
| Quản lý sản phẩm, serial, giá bán | Không | Chỉ xem; chọn serial khi giao hàng | Có |
| Nhập kho, điều chỉnh tồn kho | Không | Chỉ xem tồn kho | Có |
| Đặt hàng, tạo đơn | Cho bản thân | Tạo đơn cho khách | Có |
| Xem đơn hàng | Đơn của mình | Đơn chờ tiếp nhận và đơn được giao | Toàn bộ |
| Xác nhận, cập nhật đơn hàng | Không | Đơn được giao | Toàn bộ |
| Hủy đơn hàng | Đơn của mình chưa xác nhận | Theo quyền xử lý và trạng thái đơn | Theo quy tắc nghiệp vụ |
| Quản lý hồ sơ khách hàng | Hồ sơ của mình | Tra cứu, thêm, cập nhật phục vụ bán hàng | Toàn bộ |
| Đặt lịch tư vấn, xem thử | Cho bản thân | Tạo lịch cho khách | Có |
| Xử lý lịch hẹn | Không | Lịch được giao | Toàn bộ |
| Tra cứu bảo hành | Đồng hồ của mình | Phục vụ bán hàng | Toàn bộ |
| Lập phiếu bảo hành | Không | Có | Có |
| Quản lý khuyến mãi, nếu có | Không | Không | Có |
| Xem thống kê | Không | Doanh số của mình | Toàn hệ thống |
| Sao lưu, phục hồi dữ liệu | Không | Không | Có |

Quyền truy cập phải được kiểm tra tại phía máy chủ, bao gồm vai trò, chủ sở hữu dữ liệu và phạm vi phụ trách. Người đăng ký mới chỉ được cấp vai trò Khách hàng. Mật khẩu được lưu dưới dạng băm có salt bằng cơ chế phù hợp của .NET.

## 📋 Quy tắc nghiệp vụ dự kiến

### 1. Sản phẩm, serial & Tồn kho

- **Mẫu đồng hồ:** Lưu tên, thương hiệu, loại máy (Automatic, Quartz, Smartwatch), chất liệu, thông số, hình ảnh, giá niêm yết và thời hạn bảo hành.
- **Từng chiếc đồng hồ:** Liên kết với một mẫu; có serial duy nhất, tình trạng, giấy tờ và trạng thái kho. Admin có thể thiết lập giá riêng theo chiếc nếu nghiệp vụ yêu cầu.
- Tồn kho khả dụng được tính từ các chiếc đủ điều kiện bán, chưa bị giữ cho đơn khác. Giá và tồn kho phải được kiểm tra lại khi đặt và xác nhận đơn.
- Một chiếc không được gán đồng thời cho nhiều đơn hàng còn hiệu lực.
- Mọi lần nhập hoặc điều chỉnh kho phải ghi nhận người thực hiện, thời gian, serial liên quan và lý do.

### 2. Đơn hàng & Hóa đơn

Luồng đơn hàng trực tuyến cơ bản:

```text
Chờ xác nhận → Đã xác nhận → Đang giao → Hoàn tất
      ↓
    Đã hủy
```

- Khách hàng chỉ được hủy đơn của mình khi đơn ở trạng thái **Chờ xác nhận**.
- Khi xác nhận đơn, hệ thống kiểm tra và giữ đủ hàng; nhân viên gán serial cụ thể trước khi giao. Việc gán serial phải sử dụng phần hàng đã giữ, tránh trừ tồn hai lần.
- Việc hủy đơn đã xác nhận do nhân viên hoặc Admin xử lý phải tuân theo quy tắc nghiệp vụ, ghi nhận lý do và giải phóng hàng đã giữ. Đơn đã giao hoặc hoàn tất không được tùy ý chuyển thành đã hủy.
- Đơn bán trực tiếp có thể chuyển sang **Hoàn tất** sau khi ghi nhận thanh toán và bàn giao đồng hồ.
- Trạng thái thanh toán được lưu riêng với trạng thái xử lý đơn hàng.
- Chi tiết đơn hàng lưu giá bán thực tế tại thời điểm giao dịch; nhân viên không được tự sửa giá. Đơn có giá riêng theo serial phải được chốt giá trước khi xác nhận bán.
- Hóa đơn liên kết với đơn hàng và được lập khi hoàn tất giao dịch theo phạm vi mô phỏng của đồ án. Thay đổi giá sản phẩm hoặc hồ sơ khách sau này không được làm thay đổi nội dung hóa đơn đã lập.
- Việc cập nhật đơn hàng, chi tiết và kho phải nhất quán, có cơ chế khôi phục khi ghi dữ liệu thất bại và kiểm soát khi nhiều người cùng thao tác.

### 3. Lịch hẹn & Bảo hành

- Lịch hẹn lưu khách hàng, thời gian, nội dung tư vấn hoặc mẫu muốn xem, nhân viên phụ trách và trạng thái xử lý.
- Trước khi xác nhận lịch, nhân viên kiểm tra khả năng phục vụ và tình trạng đồng hồ muốn xem. Đặt lịch không tự động giữ hàng.
- Phiếu bảo hành liên kết với khách hàng, chiếc đồng hồ theo serial và giao dịch đã hoàn tất.
- Phiếu bảo hành lưu ngày bắt đầu, ngày kết thúc và điều kiện bảo hành; khách chỉ được xem bảo hành thuộc các giao dịch của mình.

### 4. Thống kê

- Doanh thu và doanh số nhân viên được tính từ các đơn đã hoàn tất trong khoảng thời gian chọn; đơn đã hủy không được tính.
- Doanh số nhân viên được xác định theo người phụ trách được lưu trên đơn.
- Báo cáo sản phẩm bán chạy tổng hợp số lượng bán theo mẫu đồng hồ.
- Báo cáo tồn kho phân biệt hàng khả dụng, hàng đang giữ và hàng không đủ điều kiện bán.

## 🧩 Ứng dụng công nghệ XML

| Công nghệ | Ứng dụng trong đồ án |
| --- | --- |
| XML | Lưu tài khoản, khách hàng, sản phẩm, serial, đơn hàng, hóa đơn, kho, lịch hẹn và bảo hành |
| XSD | Kiểm tra cấu trúc, kiểu dữ liệu, trường bắt buộc và các ràng buộc được khai báo |
| LINQ to XML / XML DOM | Đọc, thêm, sửa và xóa dữ liệu XML |
| XPath | Tra cứu, tìm kiếm và lọc dữ liệu theo điều kiện |
| XSLT → HTML | Xuất hóa đơn, báo cáo doanh thu và danh sách sản phẩm bán chạy |
| RSS 2.0 / Web Service | Đọc tin tức đồng hồ hoặc cung cấp dịch vụ tra cứu bảo hành có kiểm soát truy cập |
| SQL Server ↔ XML | Xuất, nhập dữ liệu phục vụ sao lưu và phục hồi |

XSD được kết hợp với kiểm tra nghiệp vụ tại tầng dịch vụ để kiểm soát quan hệ giữa các tệp XML, tồn kho và quyền truy cập. Dữ liệu nhập từ SQL hoặc bản sao lưu phải được kiểm tra trước khi phục hồi.

RSS hoặc Web Service là hạng mục tích hợp dự kiến. Chức năng tỷ giá ngoại tệ từ nguồn XML và khuyến mãi thuộc phạm vi mở rộng, thực hiện khi các chức năng chính đã ổn định.

## 📂 Cấu trúc thư mục dự kiến

```text
CN_XML_QUANLYBANDONGHO/
├── src/                              # Mã nguồn website C# / ASP.NET Core MVC
│   ├── Controllers/                  # Tiếp nhận yêu cầu và điều phối xử lý
│   ├── Models/                       # Thực thể và mô hình dữ liệu
│   ├── Views/                        # Giao diện khách hàng, nhân viên, Admin
│   ├── Services/                     # Nghiệp vụ, XML, XPath, XSLT, RSS, SQL
│   └── wwwroot/                      # CSS, JavaScript và tài nguyên công khai
├── Data_XML/                         # Dữ liệu nghiệp vụ, không công khai qua web
│   ├── TaiKhoan.xml
│   ├── KhachHang.xml
│   ├── DiaChiNhanHang.xml
│   ├── HangSanXuat.xml
│   ├── DongHo.xml                     # Danh mục mẫu đồng hồ
│   ├── ChiTietDongHo.xml              # Từng chiếc đồng hồ theo serial
│   ├── GioHang.xml
│   ├── DonHang.xml
│   ├── ChiTietDonHang.xml
│   ├── HoaDon.xml
│   ├── ChiTietHoaDon.xml
│   ├── LichHen.xml
│   ├── PhieuBaoHanh.xml
│   ├── PhieuNhap.xml
│   ├── ChiTietPhieuNhap.xml
│   └── NhatKyKho.xml
├── Schemas_XSD/                      # Lược đồ kiểm tra dữ liệu XML
├── Templates_XSLT/
│   ├── PhieuHoaDon.xslt
│   ├── BaoCaoDoanhThu.xslt
│   └── SanPhamBanChay.xslt
├── Docs/                             # Báo cáo, slide, thiết kế, hướng dẫn, SQL
└── README.md
```

Các tệp XML liên kết bằng mã định danh thống nhất. `DongHo.xml` lưu thông tin mẫu; `ChiTietDongHo.xml` lưu từng chiếc. Đơn hàng theo dõi quá trình bán; hóa đơn lưu thông tin giao dịch đã chốt và liên kết về đơn hàng gốc.

Dữ liệu tài khoản, khách hàng, đơn hàng và giấy tờ riêng tư phải được lưu ngoài thư mục tài nguyên công khai. Chỉ đưa dữ liệu mẫu vào repository; không commit mật khẩu, chuỗi kết nối hoặc dữ liệu cá nhân thực.

## 🛠️ Công nghệ sử dụng dự kiến

- **Ngôn ngữ:** C#.
- **Nền tảng Web:** ASP.NET Core MVC; thống nhất phiên bản .NET khi khởi tạo dự án.
- **Giao diện:** HTML, CSS, JavaScript, Bootstrap.
- **Công nghệ XML:** XML 1.0, XSD, XPath, XSLT, LINQ to XML, `XmlDocument`.
- **Tích hợp:** RSS 2.0 qua `SyndicationFeed` / `XDocument`, hoặc Web API tra cứu bảo hành.
- **Chuyển đổi dữ liệu:** SQL Server ↔ XML.
- **Công cụ:** Visual Studio / Visual Studio Code, SQL Server, Git và GitHub.

## 💻 Cài đặt & Làm việc nhóm với Git

### 1. Clone repository

```bash
git clone https://github.com/nguyenhuuhiep386-dotcom/CN_XML_QUANLYBANDONGHO.git
cd CN_XML_QUANLYBANDONGHO
```

### 2. Chuẩn bị môi trường chạy

Sau khi mã nguồn được khởi tạo:

1. Cài .NET SDK tương ứng với phiên bản trong tệp dự án `.csproj` hoặc `global.json` nếu có.
2. Mở solution hoặc project bằng công cụ phát triển phù hợp.
3. Khôi phục các gói NuGet và cấu hình đường dẫn dữ liệu XML, XSD, XSLT.
4. Chuẩn bị dữ liệu mẫu; cấu hình SQL Server nếu sử dụng chức năng chuyển đổi dữ liệu.
5. Build và chạy project Web; kiểm thử bằng tài khoản mẫu của từng vai trò.

Mở `AurelMaison.slnx` hoặc `AurelMaison/AurelMaison.csproj`. Chạy profile **http**, hoặc dùng lệnh sau ở thư mục gốc repo.

```bash
dotnet run --project AurelMaison/AurelMaison.csproj --launch-profile http
```

Trang chủ là `http://localhost:5269`. Đăng nhập bằng tài khoản mẫu `khach1@gmail.com` hoặc `khach2@gmail.com`, mật khẩu `123456`. Website đọc XML mẫu có sẵn trong `AurelMaison/App_Data/Xml`. Giữ bản sao thư mục này nếu cần đặt lại dữ liệu demo sau khi thử.

#### Giao diện khách hàng

Bản giao diện dùng chung `Views/Shared/_CustomerLayout.cshtml`, `wwwroot/css/customer.css` và `wwwroot/js/customer.js`. Các trang có lưới sản phẩm, menu thu gọn cho màn hình nhỏ, bố cục giỏ hàng và đặt hàng với phần tóm tắt bên cạnh, cùng ba tab tài khoản. Ảnh banner và năm ảnh đồng hồ là ảnh minh họa tạo bằng AI để thử bố cục, cần thay bằng ảnh sản phẩm thực tế khi triển khai.

#### Kiểm tra phần khách hàng

- Tìm DH001 trong **Bộ sưu tập**, thêm vào giỏ khi chưa đăng nhập rồi đặt hàng. Sau khi đăng nhập, kiểm tra sản phẩm vẫn trong giỏ. Thử chọn nhận tại showroom, lưu mã đơn rồi mở lại trong **Đơn hàng của tôi**.
- Dùng khách 2 để kiểm tra đơn `AM20260018` đang chờ xác nhận và quyền truy cập chi tiết đơn. Đặt lịch từ chi tiết DH001 vào một ngày tương lai lúc 10:00, rồi kiểm tra tab **Lịch hẹn của tôi**.
- Mở `/DanhMuc/Xml` để xem HTML từ `DanhMucDongHo.xslt`. Trang **Bảo hành của tôi** gọi `/api/baohanh/{maPhieu}`. Khi dùng API hiện tại trả 501, trang sẽ báo chờ tích hợp.

#### Điểm nối khi ghép với các thành viên

- `Services/Interfaces.cs` là ranh giới của phần khách hàng. Giao diện khách dùng `CustomerGroupAdapter` cho tài khoản, hồ sơ, danh mục và kho; `CustomerOrderAdapter` nối `IDonHangService` với dịch vụ đơn hàng thật của Nguyên. `Services/CustomerDemo` không được đăng ký khi chạy ứng dụng. `LichHenService` của Hiệp phục vụ cả khách và Staff. XSD đơn/chi tiết dùng `xs:all` để nhận cột serial và nhân viên bổ sung khi chuyển trạng thái, đồng thời kiểm tra khóa trùng.
- `LichHenService` dùng cùng khóa của `XmlHelper` cho cả đọc, kiểm tra, sinh mã và ghi. Khi nối khu nhân viên, truyền mã nhân viên từ phiên đăng nhập cho thao tác xác nhận, hoàn tất hoặc hủy lịch.
- `WarrantyClient` dùng URL gốc trong `Warranty:BaseUrl` và tên cookie trong `Security.CookieName`. Đồng bộ hai cấu hình này khi ghép API của Nguyên. Profile HTTPS cần chứng chỉ phát triển được tin cậy.

### 3. Tạo nhánh riêng cho từng chức năng

Trước khi bắt đầu công việc mới, bảo đảm các thay đổi hiện tại đã được lưu trên nhánh đang làm, sau đó cập nhật `main` và tạo nhánh:

```bash
git switch main
git pull --ff-only origin main
git switch -c feature/nguyen-quan-ly-dong-ho
```

Ví dụ tên nhánh:

- `feature/hiep-dat-hang`
- `feature/nguyen-quan-ly-dong-ho`
- `feature/phuong-bao-cao-xslt`

### 4. Lưu và đẩy thay đổi lên nhánh cá nhân

Kiểm tra nội dung trước khi commit; thay tên nhánh trong ví dụ bằng nhánh đang làm:

```bash
git status
git diff
git add -p
git diff --cached
git commit -m "Nguyen: Them quan ly dong ho va serial"
git push -u origin feature/nguyen-quan-ly-dong-ho
```

`git add -p` chọn các thay đổi trong tệp đã được Git theo dõi. Với tệp mới, dùng `git add` kèm đường dẫn cụ thể trước khi kiểm tra phần đã stage.

### 5. Tạo Pull Request và tích hợp

- Tạo Pull Request từ nhánh chức năng vào `main` trên GitHub.
- Ghi rõ chức năng thay đổi, tệp XML/XSD liên quan và cách kiểm thử.
- Nhờ thành viên khác kiểm tra trước khi hợp nhất.
- Khi có xung đột, phối hợp với người phụ trách để xử lý và kiểm thử lại trước khi merge.
- Thống nhất các thay đổi cấu trúc XML dùng chung trước khi tích hợp vào `main`.

## ✅ Kịch bản kiểm thử chính

- Đăng ký tài khoản Khách hàng, đăng nhập và kiểm tra giới hạn quyền của ba vai trò.
- Tìm kiếm, lọc đồng hồ theo thương hiệu, giá, loại máy và chất liệu.
- Đặt hàng hợp lệ; từ chối số lượng vượt tồn kho khả dụng.
- Khách chỉ xem và hủy được đơn của mình theo đúng trạng thái cho phép.
- Hai nhân viên không thể bán hoặc gán cùng một serial cho hai đơn đồng thời.
- Hoàn tất đơn bán trực tiếp và đơn trực tuyến; cập nhật đúng trạng thái kho.
- Đơn bị hủy hợp lệ giải phóng hàng đã giữ; lỗi ghi XML không để dữ liệu cập nhật dở dang.
- Thay đổi giá hoặc thông tin khách không làm sai nội dung hóa đơn đã lập.
- Đặt, xác nhận và tra cứu lịch hẹn theo phạm vi quyền.
- Lập bảo hành đúng serial và khách hàng; ngăn khách xem bảo hành của người khác.
- Xuất hóa đơn HTML bằng XSLT; đối chiếu báo cáo doanh thu với đơn hoàn tất.
- RSS / Web Service xử lý được mất kết nối hoặc dữ liệu đầu vào không hợp lệ.
- Sao lưu và phục hồi giữ được dữ liệu cùng các liên kết giữa các tệp.

---

*© 2026 — Nhóm Đồ Án Công Nghệ XML: Nguyễn Hữu Hiệp — Đặng Thái Nguyên — Võ Văn Phương.*
