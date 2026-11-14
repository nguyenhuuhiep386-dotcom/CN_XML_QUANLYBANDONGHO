namespace AurelMaison.Models;

// Không nhận giá từ trình duyệt. MaKH phải lấy từ phiên đăng nhập ở controller.
public record MucDatHang(string MaMau, int SoLuong);

public record ThongTinNhanHang(string TenNguoiNhan, string SoDienThoai,
    string HinhThucNhan, string? DiaChiNhan = null, string? Showroom = null);
