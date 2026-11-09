using System.Xml.Linq;

namespace AurelMaison.Models;

/// <summary>Một dòng của TaiKhoan.xml. Dùng làm kiểu cho PasswordHasher&lt;TaiKhoan&gt;.</summary>
public class TaiKhoan
{
    public string MaTK { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string MatKhauHash { get; set; } = string.Empty;
    public string VaiTro { get; set; } = string.Empty;
    public string TrangThai { get; set; } = string.Empty;
    public DateTime NgayTao { get; set; }

    public static TaiKhoan TuXml(XElement dong) => new()
    {
        MaTK = (string?)dong.Element("MaTK") ?? string.Empty,
        Email = (string?)dong.Element("Email") ?? string.Empty,
        MatKhauHash = (string?)dong.Element("MatKhauHash") ?? string.Empty,
        VaiTro = (string?)dong.Element("VaiTro") ?? string.Empty,
        TrangThai = (string?)dong.Element("TrangThai") ?? string.Empty,
        NgayTao = DateTime.TryParse((string?)dong.Element("NgayTao"), out var ngay) ? ngay : DateTime.MinValue
    };
}

/// <summary>Kết quả đăng nhập thành công: mã tài khoản, vai trò và mã hồ sơ đi kèm.</summary>
public class ThongTinDangNhap
{
    public string MaTK { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string VaiTro { get; set; } = string.Empty;
    public string HoTen { get; set; } = string.Empty;

    /// <summary>Có khi VaiTro = KhachHang.</summary>
    public string? MaKH { get; set; }

    /// <summary>Có khi VaiTro = NhanVien hoặc Admin.</summary>
    public string? MaNV { get; set; }
}
