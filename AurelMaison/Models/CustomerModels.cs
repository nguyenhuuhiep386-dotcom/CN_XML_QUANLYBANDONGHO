using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace AurelMaison.Models;

public static class Security
{
    public const string CookieName = "AurelMaison.Auth";
    public const string Customer = global::AurelMaison.Models.VaiTro.KhachHang;
    public const string Staff = global::AurelMaison.Models.VaiTro.NhanVien;
    public const string Admin = global::AurelMaison.Models.VaiTro.Admin;
}

public static class Rules
{
    public const string Pending = TrangThaiDon.ChoXacNhan;
    public const string Confirmed = TrangThaiDon.DaXacNhan;
    public const string Done = TrangThaiDon.HoanTat;
    public const string Cancelled = TrangThaiDon.DaHuy;
    public const string Delivery = HinhThucNhan.GiaoTanNoi;
    public const string Pickup = HinhThucNhan.NhanTaiShowroom;
    public const string Showroom = "Showroom Đà Nẵng";
    public static readonly TimeSpan VietnamOffset = TimeSpan.FromHours(7);
    public static DateTime Now => DateTime.UtcNow.Add(VietnamOffset);
    public static string State(string state) => state switch
    {
        Pending => "Chờ xác nhận", Confirmed => "Đã xác nhận", Done => "Hoàn tất", Cancelled => "Đã hủy",
        "DangGiao" => "Đang giao", "ConBaoHanh" => "Còn bảo hành", "HetBaoHanh" => "Hết bảo hành", _ => state
    };
    public static string Money(decimal amount) => amount.ToString("N0", System.Globalization.CultureInfo.GetCultureInfo("vi-VN")) + " ₫";
}

internal static class CustomerResult
{
    internal static KetQua Ok(string message, string? id = null) =>
        new() { ThanhCong = true, ThongBao = message, Ma = id ?? "" };
    internal static KetQua Loi(string message) => new() { ThongBao = message };
}

public record CustomerIdentity(string MaTK, string Email, string Role, string? MaKH, string? MaNV, string HoTen);
public record HoSo(string MaKH, string HoTen, string? SoDienThoai, string? DiaChi);
public record Hang(string MaHang, string TenHang);
public record DongHo(string MaMau, string TenMau, string MaHang, string TenHang, decimal Gia, string LoaiMay,
    decimal KichThuoc, string ChatLieu, string MoTa, string HinhAnh, int ThoiHanBaoHanh, int SoConHang);
public record LocDongHo(string? TuKhoa, string? Hang, decimal? GiaTu, decimal? GiaDen, string? LoaiMay, string? ChatLieu);
public record DanhMucVm(LocDongHo Loc, IReadOnlyList<DongHo> Mau, IReadOnlyList<Hang> Hang,
    IReadOnlyList<string> LoaiMay, IReadOnlyList<string> ChatLieu);
public record TrangChuVm(IReadOnlyList<Hang> Hang, IReadOnlyList<DongHo> NoiBat);
public record MucGio(string MaMau, int SoLuong);
public record DongGio(string MaMau, string TenMau, string HinhAnh, decimal Gia, int SoLuong, int SoConHang, bool DangBan);
public record GioVm(IReadOnlyList<DongGio> Dong)
{
    public decimal TongTien => Dong.Sum(m => m.Gia * m.SoLuong);
    public bool HopLe => Dong.Count > 0 && Dong.All(m => m.DangBan && m.SoLuong <= m.SoConHang);
}

public sealed class DangKyForm
{
    [Required(ErrorMessage = "Nhập họ tên."), StringLength(100)] public string HoTen { get; set; } = "";
    [Required(ErrorMessage = "Nhập email."), EmailAddress(ErrorMessage = "Email không hợp lệ."), StringLength(100)] public string Email { get; set; } = "";
    [Required, StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải có từ 6 đến 100 ký tự."), DataType(DataType.Password)] public string MatKhau { get; set; } = "";
    [Required, Compare(nameof(MatKhau), ErrorMessage = "Mật khẩu nhập lại không khớp."), DataType(DataType.Password)] public string NhapLai { get; set; } = "";
}

public sealed class DangNhapForm
{
    [Required, EmailAddress, StringLength(100)] public string Email { get; set; } = "";
    [Required, StringLength(100), DataType(DataType.Password)] public string MatKhau { get; set; } = "";
    public string? ReturnUrl { get; set; }
}

public sealed class HoSoForm
{
    [Required, StringLength(100)] public string HoTen { get; set; } = "";
    [RegularExpression(@"^\+?[0-9]{8,14}$", ErrorMessage = "Số điện thoại không hợp lệ."), StringLength(15)] public string? SoDienThoai { get; set; }
    [StringLength(250)] public string? DiaChi { get; set; }
}

public sealed class DoiMatKhauForm
{
    [Required, StringLength(100), DataType(DataType.Password)] public string MatKhauCu { get; set; } = "";
    [Required, StringLength(100, MinimumLength = 6), DataType(DataType.Password)] public string MatKhauMoi { get; set; } = "";
    [Required, Compare(nameof(MatKhauMoi), ErrorMessage = "Mật khẩu nhập lại không khớp."), DataType(DataType.Password)] public string NhapLai { get; set; } = "";
}

public sealed class DatHangForm : IValidatableObject
{
    [Required, StringLength(100)] public string TenNguoiNhan { get; set; } = "";
    [Required, StringLength(15), RegularExpression(@"^\+?[0-9]{8,14}$", ErrorMessage = "Số điện thoại không hợp lệ.")] public string SoDienThoai { get; set; } = "";
    public string HinhThucNhan { get; set; } = Rules.Delivery;
    [StringLength(250)] public string? DiaChiNhan { get; set; }
    [StringLength(100)] public string? Showroom { get; set; }
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (HinhThucNhan == Rules.Delivery && string.IsNullOrWhiteSpace(DiaChiNhan))
            yield return new ValidationResult("Nhập địa chỉ giao hàng.", [nameof(DiaChiNhan)]);
        else if (HinhThucNhan == Rules.Pickup && Showroom != Rules.Showroom)
            yield return new ValidationResult("Chọn showroom nhận hàng.", [nameof(Showroom)]);
        else if (HinhThucNhan is not (Rules.Delivery or Rules.Pickup))
            yield return new ValidationResult("Hình thức nhận hàng không hợp lệ.", [nameof(HinhThucNhan)]);
    }
}

public sealed class DatLichForm
{
    public string? MaMau { get; set; }
    [Required, StringLength(100)] public string Showroom { get; set; } = Rules.Showroom;
    [Required] public DateTime? NgayHen { get; set; }
    [Required, StringLength(100)] public string HoTen { get; set; } = "";
    [Required, StringLength(15), RegularExpression(@"^\+?[0-9]{8,14}$", ErrorMessage = "Số điện thoại không hợp lệ.")] public string SoDienThoai { get; set; } = "";
    [StringLength(250)] public string? GhiChu { get; set; }
}

public record LichHen(string MaLH, string MaKH, string? MaMau, string Showroom, DateTime NgayHen, string HoTen,
    string SoDienThoai, string? GhiChu, string TrangThai, string? MaNVXuLy, string? LyDoHuy);
public record DonHang(string MaDon, string MaKH, DateTime NgayDat, string TrangThai, decimal TongTien,
    string TenNguoiNhan, string SoDienThoai, string HinhThucNhan, string? DiaChiNhan, string? Showroom,
    string? LyDoHuy, IReadOnlyList<ChiTietDon> Dong);
public record ChiTietDon(string MaMau, string TenMau, decimal DonGia);
public record BaoHanh(string MaPhieu, string MaDon, string TenMau, string Serial, DateOnly NgayBatDau, DateOnly NgayHetHan, string TrangThai);
public record TraCuuVm(string MaPhieu, BaoHanh? Phieu = null, string? Loi = null);
public record TaiKhoanVm(string Tab, HoSoForm HoSo, DoiMatKhauForm MatKhau, IReadOnlyList<LichHen> Lich);
