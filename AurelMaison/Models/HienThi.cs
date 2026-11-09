using System.Globalization;

namespace AurelMaison.Models;

/// <summary>Đổi mã trạng thái không dấu và số tiền sang chữ hiển thị tiếng Việt.</summary>
public static class HienThi
{
    private static readonly CultureInfo Vi = CultureInfo.GetCultureInfo("vi-VN");

    /// <summary>285000000 → "285.000.000 ₫".</summary>
    public static string Tien(decimal soTien) => soTien.ToString("#,0", Vi) + " ₫";

    /// <summary>4820000000 → "4,82 tỷ ₫"; 401700000 → "401,7 triệu ₫".</summary>
    public static string TienGon(decimal soTien) => soTien switch
    {
        >= 1_000_000_000m => (soTien / 1_000_000_000m).ToString("0.##", Vi) + " tỷ ₫",
        >= 1_000_000m => (soTien / 1_000_000m).ToString("0.#", Vi) + " triệu ₫",
        _ => Tien(soTien)
    };

    public static string So(decimal so, string dinhDang = "#,0.##") => so.ToString(dinhDang, Vi);

    public static string Ngay(DateTime ngay) => ngay.ToString("dd/MM/yyyy");

    public static string NgayGio(DateTime ngay) => ngay.ToString("dd/MM/yyyy HH:mm");

    public static string VaiTro(string? ma) => ma switch
    {
        Models.VaiTro.KhachHang => "Khách hàng",
        Models.VaiTro.NhanVien => "Nhân viên",
        Models.VaiTro.Admin => "Admin",
        _ => ma ?? string.Empty
    };

    public static string TrangThaiTaiKhoan(string? ma) => ma switch
    {
        Models.TrangThaiTaiKhoan.HoatDong => "Hoạt động",
        Models.TrangThaiTaiKhoan.Khoa => "Đã khóa",
        _ => ma ?? string.Empty
    };

    public static string TrangThaiMau(string? ma) => ma switch
    {
        Models.TrangThaiMau.DangBan => "Đang bán",
        Models.TrangThaiMau.NgungBan => "Ngừng bán",
        _ => ma ?? string.Empty
    };

    public static string TrangThaiChiec(string? ma) => ma switch
    {
        Models.TrangThaiChiec.ConHang => "Còn hàng",
        Models.TrangThaiChiec.DaGiu => "Đã giữ",
        Models.TrangThaiChiec.DaBan => "Đã bán",
        Models.TrangThaiChiec.KhongBan => "Không bán",
        _ => ma ?? string.Empty
    };

    public static string TinhTrang(string? ma) => ma switch
    {
        Models.TinhTrangChiec.Moi => "Mới",
        Models.TinhTrangChiec.DaSuDung => "Đã sử dụng",
        _ => ma ?? string.Empty
    };

    /// <summary>"Mới · Đủ bộ", "Đã sử dụng · Chỉ hộp"...</summary>
    public static string TinhTrangDayDu(string tinhTrang, bool coHop, bool coGiayTo) =>
        $"{TinhTrang(tinhTrang)} · " + (coHop, coGiayTo) switch
        {
            (true, true) => "Đủ bộ",
            (true, false) => "Chỉ hộp",
            (false, true) => "Chỉ giấy tờ",
            _ => "Không hộp, giấy"
        };
}
