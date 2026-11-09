using AurelMaison.Models;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace AurelMaison.Services;

/// <summary>Một dòng KhachHang.xml. MaTK null = khách tạo tại quầy, chưa có tài khoản.</summary>
public class KhachHang
{
    public string MaKH { get; set; } = string.Empty;
    public string? MaTK { get; set; }
    public string HoTen { get; set; } = string.Empty;
    public string? SoDienThoai { get; set; }
    public string? Email { get; set; }
    public string? DiaChi { get; set; }
    public DateTime NgayTao { get; set; }

    public static KhachHang TuXml(XElement e) => new()
    {
        MaKH = (string?)e.Element("MaKH") ?? string.Empty,
        MaTK = (string?)e.Element("MaTK"),
        HoTen = (string?)e.Element("HoTen") ?? string.Empty,
        SoDienThoai = (string?)e.Element("SoDienThoai"),
        Email = (string?)e.Element("Email"),
        DiaChi = (string?)e.Element("DiaChi"),
        NgayTao = DateTime.TryParse((string?)e.Element("NgayTao"), out var n) ? n : DateTime.MinValue
    };
}

/// <summary>Hồ sơ khách hàng: tra cứu tại quầy, thêm khách tại quầy, cập nhật hồ sơ.</summary>
public class KhachHangService
{
    private static readonly Regex SoDienThoaiHopLe = new(@"^\d{9,15}$", RegexOptions.Compiled);

    public KhachHang? LayTheoMa(string? maKH)
    {
        if (string.IsNullOrEmpty(maKH))
        {
            return null;
        }
        var e = XmlHelper.TimTheoMa("KhachHang", "MaKH", maKH);
        return e == null ? null : KhachHang.TuXml(e);
    }

    /// <summary>Hồ sơ của tài khoản đang đăng nhập (trang Tài khoản của tôi).</summary>
    public KhachHang? LayTheoTaiKhoan(string? maTK) =>
        string.IsNullOrEmpty(maTK) ? null : DocTatCa().FirstOrDefault(k => k.MaTK == maTK);

    /// <summary>Nhân viên tra khách khi tạo đơn tại quầy. Bỏ khoảng trắng, dấu chấm, gạch trong số nhập vào.</summary>
    public KhachHang? TimTheoSoDienThoai(string? soDienThoai)
    {
        var sdt = ChuanHoaSdt(soDienThoai);
        return sdt == null ? null : DocTatCa().FirstOrDefault(k => k.SoDienThoai == sdt);
    }

    /// <summary>Khách mua tại quầy chưa có tài khoản: MaTK để trống. Trùng số điện thoại thì báo mã khách đã có. Ma = MaKH.</summary>
    public KetQua ThemKhachTaiQuay(string hoTen, string soDienThoai, string? email = null, string? diaChi = null)
    {
        hoTen = (hoTen ?? string.Empty).Trim();
        var sdt = ChuanHoaSdt(soDienThoai);
        email = string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();
        diaChi = string.IsNullOrWhiteSpace(diaChi) ? null : diaChi.Trim();

        var loi = KiemTra(hoTen, sdt, diaChi);
        if (loi != null)
        {
            return ThatBai(loi);
        }
        if (sdt == null)
        {
            return ThatBai("Số điện thoại bắt buộc với khách tại quầy, gồm 9–15 chữ số.");
        }
        if (email != null && !AuthService.EmailHopLe(email))
        {
            return ThatBai("Email không hợp lệ.");
        }

        return XmlHelper.ChayTrongKhoa(() =>
        {
            var daCo = TimTheoSoDienThoai(sdt);
            if (daCo != null)
            {
                return new KetQua { ThanhCong = false, Ma = daCo.MaKH, ThongBao = $"Số điện thoại đã thuộc khách {daCo.MaKH} ({daCo.HoTen})." };
            }
            var maKH = XmlHelper.SinhMa("KH", "KhachHang", "MaKH");
            return XmlHelper.Them("KhachHang", "MaKH", new XElement("KhachHang",
                new XElement("MaKH", maKH),
                new XElement("HoTen", hoTen),
                new XElement("SoDienThoai", sdt),
                email == null ? null : new XElement("Email", email),
                diaChi == null ? null : new XElement("DiaChi", diaChi),
                new XElement("NgayTao", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"))));
        });
    }

    /// <summary>Khách tự sửa hồ sơ. Email gắn với tài khoản đăng nhập nên không đổi ở đây.</summary>
    public KetQua CapNhatHoSo(string maKH, string hoTen, string? soDienThoai, string? diaChi)
    {
        hoTen = (hoTen ?? string.Empty).Trim();
        var sdt = ChuanHoaSdt(soDienThoai);
        if (!string.IsNullOrWhiteSpace(soDienThoai) && sdt == null)
        {
            return ThatBai("Số điện thoại gồm 9–15 chữ số.");
        }
        diaChi = string.IsNullOrWhiteSpace(diaChi) ? null : diaChi.Trim();
        var loi = KiemTra(hoTen, sdt, diaChi);
        if (loi != null)
        {
            return ThatBai(loi);
        }

        return XmlHelper.ChayTrongKhoa(() =>
        {
            var trung = sdt == null ? null : TimTheoSoDienThoai(sdt);
            if (trung != null && trung.MaKH != maKH)
            {
                return ThatBai("Số điện thoại đã được khách khác sử dụng.");
            }
            return XmlHelper.Sua("KhachHang", "MaKH", maKH, dong =>
            {
                dong.SetElementValue("HoTen", hoTen);
                dong.SetElementValue("SoDienThoai", sdt);
                dong.SetElementValue("DiaChi", diaChi);
            });
        });
    }

    private static List<KhachHang> DocTatCa() =>
        XmlHelper.Load("KhachHang").Root!.Elements("KhachHang").Select(KhachHang.TuXml).ToList();

    private static string? ChuanHoaSdt(string? sdt)
    {
        var so = Regex.Replace(sdt ?? string.Empty, @"[\s.\-()]", "");
        return SoDienThoaiHopLe.IsMatch(so) ? so : null;
    }

    private static string? KiemTra(string hoTen, string? sdt, string? diaChi)
    {
        if (hoTen.Length is 0 or > 100) return "Họ tên không được trống và tối đa 100 ký tự.";
        if (diaChi?.Length > 250) return "Địa chỉ tối đa 250 ký tự.";
        return null;
    }

    private static KetQua ThatBai(string thongBao) => new() { ThanhCong = false, ThongBao = thongBao };
}
