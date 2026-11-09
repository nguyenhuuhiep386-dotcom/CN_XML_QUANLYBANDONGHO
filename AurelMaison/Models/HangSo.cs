namespace AurelMaison.Models;

// Mã trạng thái lưu không dấu trong XML/SQL; giao diện tự hiển thị tiếng Việt.

public static class VaiTro
{
    public const string KhachHang = "KhachHang";
    public const string NhanVien = "NhanVien";
    public const string Admin = "Admin";

    /// <summary>Dùng cho [Authorize(Roles = ...)] ở các màn hình nhân viên.</summary>
    public const string NhanVienHoacAdmin = NhanVien + "," + Admin;
}

public static class TrangThaiTaiKhoan
{
    public const string HoatDong = "HoatDong";
    public const string Khoa = "Khoa";
}

public static class TrangThaiMau
{
    public const string DangBan = "DangBan";
    public const string NgungBan = "NgungBan";
}

/// <summary>Trạng thái từng chiếc. Tồn khả dụng = số chiếc ConHang, không lưu cột số lượng riêng.</summary>
public static class TrangThaiChiec
{
    public const string ConHang = "ConHang";
    public const string DaGiu = "DaGiu";
    public const string DaBan = "DaBan";
    public const string KhongBan = "KhongBan";
}

public static class TinhTrangChiec
{
    public const string Moi = "Moi";
    public const string DaSuDung = "DaSuDung";
}

/// <summary>ChoXacNhan → DaXacNhan → (DangGiao) → HoanTat, hoặc DaHuy.</summary>
public static class TrangThaiDon
{
    public const string ChoXacNhan = "ChoXacNhan";
    public const string DaXacNhan = "DaXacNhan";
    public const string DangGiao = "DangGiao";
    public const string HoanTat = "HoanTat";
    public const string DaHuy = "DaHuy";
}

public static class NguonDon
{
    public const string Online = "Online";
    public const string TaiQuay = "TaiQuay";
}

public static class HinhThucNhan
{
    public const string GiaoTanNoi = "GiaoTanNoi";
    public const string NhanTaiShowroom = "NhanTaiShowroom";
}

public static class TrangThaiLichHen
{
    public const string ChoXacNhan = "ChoXacNhan";
    public const string DaXacNhan = "DaXacNhan";
    public const string HoanTat = "HoanTat";
    public const string DaHuy = "DaHuy";
}
