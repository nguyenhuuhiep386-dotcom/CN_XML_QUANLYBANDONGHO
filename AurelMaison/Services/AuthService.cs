using AurelMaison.Models;
using Microsoft.AspNetCore.Identity;
using System.Net.Mail;
using System.Security.Claims;
using System.Xml.Linq;

namespace AurelMaison.Services;

/// <summary>Đăng ký, đăng nhập, đổi mật khẩu trên TaiKhoan.xml, KhachHang.xml, NhanVien.xml.</summary>
public class AuthService
{
    private const int DoDaiMatKhauToiThieu = 6;

    private readonly PasswordHasher<TaiKhoan> _hasher = new();

    public TaiKhoan? LayTaiKhoan(string? maTK)
    {
        if (string.IsNullOrEmpty(maTK))
        {
            return null;
        }
        var dong = XmlHelper.TimTheoMa("TaiKhoan", "MaTK", maTK);
        return dong == null ? null : TaiKhoan.TuXml(dong);
    }

    /// <summary>Trả null khi sai email/mật khẩu, tài khoản bị khóa hoặc thiếu hồ sơ đi kèm vai trò.</summary>
    public ThongTinDangNhap? DangNhap(string email, string matKhau)
    {
        email = (email ?? string.Empty).Trim();
        var dong = XmlHelper.Load("TaiKhoan").Root!.Elements("TaiKhoan")
            .FirstOrDefault(tk => string.Equals((string?)tk.Element("Email"), email, StringComparison.OrdinalIgnoreCase));
        if (dong == null)
        {
            return null;
        }

        var tk = TaiKhoan.TuXml(dong);
        // Băm mật khẩu tốn ~0,1 giây nên làm ngoài khóa.
        if (_hasher.VerifyHashedPassword(tk, tk.MatKhauHash, matKhau ?? string.Empty) == PasswordVerificationResult.Failed
            || tk.TrangThai != TrangThaiTaiKhoan.HoatDong)
        {
            return null;
        }

        var thongTin = new ThongTinDangNhap { MaTK = tk.MaTK, Email = tk.Email, VaiTro = tk.VaiTro };
        if (tk.VaiTro == VaiTro.KhachHang)
        {
            var kh = TimHoSo("KhachHang", tk.MaTK);
            thongTin.MaKH = (string?)kh?.Element("MaKH");
            thongTin.HoTen = (string?)kh?.Element("HoTen") ?? tk.Email;
            return thongTin.MaKH == null ? null : thongTin;
        }

        var nv = TimHoSo("NhanVien", tk.MaTK);
        thongTin.MaNV = (string?)nv?.Element("MaNV");
        thongTin.HoTen = (string?)nv?.Element("HoTen") ?? tk.Email;
        return thongTin.MaNV == null ? null : thongTin;
    }

    /// <summary>Luôn tạo vai trò KhachHang, kèm hồ sơ KhachHang. Ma = MaTK.</summary>
    public KetQua DangKy(string hoTen, string email, string matKhau)
    {
        hoTen = (hoTen ?? string.Empty).Trim();
        email = (email ?? string.Empty).Trim().ToLowerInvariant();

        if (hoTen.Length is 0 or > 100)
        {
            return ThatBai("Họ tên không được trống và tối đa 100 ký tự.");
        }
        if (!EmailHopLe(email))
        {
            return ThatBai("Email không hợp lệ.");
        }
        var loiMatKhau = KiemTraMatKhau(matKhau);
        if (loiMatKhau != null)
        {
            return ThatBai(loiMatKhau);
        }

        var hash = _hasher.HashPassword(new TaiKhoan(), matKhau);
        var bayGio = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");

        // Ghi hai file: kiểm tra XSD cả hai trước khi ghi file đầu tiên, file sau lỗi thì trả lại file đầu.
        return XmlHelper.ChayTrongKhoa(() =>
        {
            var docTK = XmlHelper.Load("TaiKhoan");
            if (docTK.Root!.Elements("TaiKhoan").Any(tk => string.Equals((string?)tk.Element("Email"), email, StringComparison.OrdinalIgnoreCase)))
            {
                return ThatBai("Email đã tồn tại.");
            }
            var docTKCu = new XDocument(docTK);

            var maTK = XmlHelper.SinhMa("TK", "TaiKhoan", "MaTK");
            var maKH = XmlHelper.SinhMa("KH", "KhachHang", "MaKH");
            docTK.Root.Add(new XElement("TaiKhoan",
                new XElement("MaTK", maTK),
                new XElement("Email", email),
                new XElement("MatKhauHash", hash),
                new XElement("VaiTro", VaiTro.KhachHang),
                new XElement("TrangThai", TrangThaiTaiKhoan.HoatDong),
                new XElement("NgayTao", bayGio)));

            var docKH = XmlHelper.Load("KhachHang");
            docKH.Root!.Add(new XElement("KhachHang",
                new XElement("MaKH", maKH),
                new XElement("MaTK", maTK),
                new XElement("HoTen", hoTen),
                new XElement("Email", email),
                new XElement("NgayTao", bayGio)));

            var loi = XmlHelper.KiemTraXsd("TaiKhoan", docTK).Concat(XmlHelper.KiemTraXsd("KhachHang", docKH)).ToList();
            if (loi.Count > 0)
            {
                return ThatBai(string.Join("; ", loi));
            }

            var kq = XmlHelper.Save("TaiKhoan", docTK);
            if (!kq.ThanhCong)
            {
                return kq;
            }
            kq = XmlHelper.Save("KhachHang", docKH);
            if (!kq.ThanhCong)
            {
                XmlHelper.Save("TaiKhoan", docTKCu);
                return kq;
            }
            return new KetQua { ThanhCong = true, Ma = maTK };
        });
    }

    public KetQua DoiMatKhau(string maTK, string matKhauCu, string matKhauMoi)
    {
        var tk = LayTaiKhoan(maTK);
        if (tk == null)
        {
            return ThatBai("Không tìm thấy tài khoản.");
        }
        if (_hasher.VerifyHashedPassword(tk, tk.MatKhauHash, matKhauCu ?? string.Empty) == PasswordVerificationResult.Failed)
        {
            return ThatBai("Mật khẩu cũ không đúng.");
        }
        var loiMatKhau = KiemTraMatKhau(matKhauMoi);
        if (loiMatKhau != null)
        {
            return ThatBai(loiMatKhau);
        }

        var hashMoi = _hasher.HashPassword(tk, matKhauMoi);
        return XmlHelper.ChayTrongKhoa(() =>
        {
            // Mật khẩu đã đổi ở yêu cầu khác trong lúc băm thì không ghi đè.
            if (LayTaiKhoan(maTK)?.MatKhauHash != tk.MatKhauHash)
            {
                return ThatBai("Mật khẩu vừa được thay đổi, vui lòng thử lại.");
            }
            return XmlHelper.Sua("TaiKhoan", "MaTK", maTK, dong => dong.SetElementValue("MatKhauHash", hashMoi));
        });
    }

    /// <summary>Tạo danh tính lưu trong cookie từ kết quả đăng nhập.</summary>
    public static ClaimsPrincipal TaoPrincipal(ThongTinDangNhap tt, string scheme)
    {
        var claims = new List<Claim>
        {
            new(CurrentUser.ClaimMaTK, tt.MaTK),
            new(ClaimTypes.Name, tt.HoTen),
            new(ClaimTypes.Email, tt.Email),
            new(ClaimTypes.Role, tt.VaiTro)
        };
        if (tt.MaKH != null)
        {
            claims.Add(new Claim(CurrentUser.ClaimMaKH, tt.MaKH));
        }
        if (tt.MaNV != null)
        {
            claims.Add(new Claim(CurrentUser.ClaimMaNV, tt.MaNV));
        }
        return new ClaimsPrincipal(new ClaimsIdentity(claims, scheme));
    }

    private static XElement? TimHoSo(string tenBang, string maTK) =>
        XmlHelper.Load(tenBang).Root!.Elements(tenBang).FirstOrDefault(hs => (string?)hs.Element("MaTK") == maTK);

    internal static string? KiemTraMatKhau(string? matKhau) =>
        string.IsNullOrEmpty(matKhau) || matKhau.Length < DoDaiMatKhauToiThieu
            ? $"Mật khẩu phải có ít nhất {DoDaiMatKhauToiThieu} ký tự."
            : null;

    internal static bool EmailHopLe(string email) =>
        email.Length <= 100 && MailAddress.TryCreate(email, out var dc) && dc.Address == email;

    private static KetQua ThatBai(string thongBao) => new() { ThanhCong = false, ThongBao = thongBao };
}
