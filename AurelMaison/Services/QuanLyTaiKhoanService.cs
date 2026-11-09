using AurelMaison.Models;
using Microsoft.AspNetCore.Identity;
using System.Xml.Linq;

namespace AurelMaison.Services;

/// <summary>Một dòng trên màn Tài khoản &amp; phân quyền: TaiKhoan ghép họ tên từ hồ sơ.</summary>
public class DongTaiKhoan
{
    public string MaTK { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string HoTen { get; set; } = string.Empty;
    public string VaiTro { get; set; } = string.Empty;
    public string TrangThai { get; set; } = string.Empty;
    public DateTime NgayTao { get; set; }

    /// <summary>MaKH hoặc MaNV ứng với vai trò hiện tại; null nếu chưa có hồ sơ.</summary>
    public string? MaHoSo { get; set; }
}

/// <summary>Admin tạo tài khoản nhân viên, đổi vai trò, khóa/mở khóa.</summary>
public class QuanLyTaiKhoanService
{
    private readonly PasswordHasher<TaiKhoan> _hasher = new();

    public List<DongTaiKhoan> LayDanhSach(string? vaiTro = null)
    {
        var kh = XmlHelper.Load("KhachHang").Root!.Elements("KhachHang").ToList();
        var nv = XmlHelper.Load("NhanVien").Root!.Elements("NhanVien").ToList();

        return XmlHelper.Load("TaiKhoan").Root!.Elements("TaiKhoan")
            .Select(TaiKhoan.TuXml)
            .Where(tk => string.IsNullOrEmpty(vaiTro) || tk.VaiTro == vaiTro)
            .Select(tk =>
            {
                var hsKH = kh.FirstOrDefault(e => (string?)e.Element("MaTK") == tk.MaTK);
                var hsNV = nv.FirstOrDefault(e => (string?)e.Element("MaTK") == tk.MaTK);
                var hoSo = tk.VaiTro == VaiTro.KhachHang ? hsKH : hsNV;
                return new DongTaiKhoan
                {
                    MaTK = tk.MaTK,
                    Email = tk.Email,
                    HoTen = (string?)(hoSo ?? hsKH ?? hsNV)?.Element("HoTen") ?? tk.Email,
                    VaiTro = tk.VaiTro,
                    TrangThai = tk.TrangThai,
                    NgayTao = tk.NgayTao,
                    MaHoSo = (string?)hoSo?.Element(tk.VaiTro == VaiTro.KhachHang ? "MaKH" : "MaNV")
                };
            })
            .OrderBy(d => d.MaTK)
            .ToList();
    }

    public DongTaiKhoan? LayMot(string? maTK) =>
        string.IsNullOrEmpty(maTK) ? null : LayDanhSach().FirstOrDefault(d => d.MaTK == maTK);

    /// <summary>Tạo TaiKhoan (NhanVien hoặc Admin) kèm hồ sơ NhanVien, mật khẩu tạm do Admin đặt. Ma = MaTK.</summary>
    public KetQua TaoNhanVien(string hoTen, string email, string matKhauTam, string? soDienThoai, string? chucVu, string vaiTro)
    {
        hoTen = (hoTen ?? string.Empty).Trim();
        email = (email ?? string.Empty).Trim().ToLowerInvariant();
        soDienThoai = string.IsNullOrWhiteSpace(soDienThoai) ? null : soDienThoai.Trim();
        chucVu = string.IsNullOrWhiteSpace(chucVu) ? ChucVuMacDinh(vaiTro) : chucVu.Trim();

        if (vaiTro is not (VaiTro.NhanVien or VaiTro.Admin))
        {
            return ThatBai("Chỉ tạo được tài khoản Nhân viên hoặc Admin ở đây.");
        }
        if (hoTen.Length is 0 or > 100)
        {
            return ThatBai("Họ tên không được trống và tối đa 100 ký tự.");
        }
        if (!AuthService.EmailHopLe(email))
        {
            return ThatBai("Email không hợp lệ.");
        }
        if (soDienThoai != null && !System.Text.RegularExpressions.Regex.IsMatch(soDienThoai, @"^\d{9,15}$"))
        {
            return ThatBai("Số điện thoại gồm 9–15 chữ số.");
        }
        if (chucVu.Length > 50)
        {
            return ThatBai("Chức vụ tối đa 50 ký tự.");
        }
        var loiMatKhau = AuthService.KiemTraMatKhau(matKhauTam);
        if (loiMatKhau != null)
        {
            return ThatBai(loiMatKhau);
        }

        var hash = _hasher.HashPassword(new TaiKhoan(), matKhauTam);
        var bayGio = DateTime.Now;

        return XmlHelper.ChayTrongKhoa(() =>
        {
            var docTK = XmlHelper.Load("TaiKhoan");
            if (docTK.Root!.Elements("TaiKhoan").Any(tk => string.Equals((string?)tk.Element("Email"), email, StringComparison.OrdinalIgnoreCase)))
            {
                return ThatBai("Email đã tồn tại.");
            }
            var docTKCu = new XDocument(docTK);
            var maTK = XmlHelper.SinhMa("TK", "TaiKhoan", "MaTK");
            docTK.Root.Add(new XElement("TaiKhoan",
                new XElement("MaTK", maTK),
                new XElement("Email", email),
                new XElement("MatKhauHash", hash),
                new XElement("VaiTro", vaiTro),
                new XElement("TrangThai", TrangThaiTaiKhoan.HoatDong),
                new XElement("NgayTao", bayGio.ToString("yyyy-MM-ddTHH:mm:ss"))));

            var docNV = XmlHelper.Load("NhanVien");
            docNV.Root!.Add(HoSoNhanVien(XmlHelper.SinhMa("NV", "NhanVien", "MaNV"), maTK, hoTen, soDienThoai, chucVu, bayGio));

            return GhiHaiFile(docTK, docTKCu, "NhanVien", docNV, maTK);
        });
    }

    /// <summary>
    /// Đổi vai trò và trạng thái. Admin không tự khóa hoặc tự hạ quyền chính mình.
    /// Vai trò mới chưa có hồ sơ đi kèm thì tạo hồ sơ (KhachHang hoặc NhanVien) để đăng nhập được.
    /// Người đang đăng nhập bị khóa/đổi vai trò sẽ bị đăng xuất ở yêu cầu kế tiếp (OnValidatePrincipal).
    /// </summary>
    public KetQua CapNhat(string maTK, string vaiTroMoi, string trangThaiMoi, string? maTKDangDung)
    {
        if (vaiTroMoi is not (VaiTro.KhachHang or VaiTro.NhanVien or VaiTro.Admin))
        {
            return ThatBai("Vai trò không hợp lệ.");
        }
        if (trangThaiMoi is not (TrangThaiTaiKhoan.HoatDong or TrangThaiTaiKhoan.Khoa))
        {
            return ThatBai("Trạng thái không hợp lệ.");
        }
        if (maTK == maTKDangDung && (vaiTroMoi != VaiTro.Admin || trangThaiMoi != TrangThaiTaiKhoan.HoatDong))
        {
            return ThatBai("Không thể tự khóa hoặc tự hạ quyền tài khoản đang đăng nhập.");
        }

        return XmlHelper.ChayTrongKhoa(() =>
        {
            var docTK = XmlHelper.Load("TaiKhoan");
            var dongTK = docTK.Root!.Elements("TaiKhoan").FirstOrDefault(e => (string?)e.Element("MaTK") == maTK);
            if (dongTK == null)
            {
                return ThatBai($"Không tìm thấy tài khoản {maTK}.");
            }
            var docTKCu = new XDocument(docTK);
            var email = (string?)dongTK.Element("Email") ?? string.Empty;
            dongTK.SetElementValue("VaiTro", vaiTroMoi);
            dongTK.SetElementValue("TrangThai", trangThaiMoi);

            // Hồ sơ đi kèm vai trò mới.
            var bangHoSo = vaiTroMoi == VaiTro.KhachHang ? "KhachHang" : "NhanVien";
            var docHoSo = XmlHelper.Load(bangHoSo);
            var daCoHoSo = docHoSo.Root!.Elements(bangHoSo).Any(e => (string?)e.Element("MaTK") == maTK);
            if (daCoHoSo)
            {
                var kqLuu = XmlHelper.Save("TaiKhoan", docTK);
                kqLuu.Ma = maTK;
                return kqLuu;
            }

            var hoTen = TimHoTen(maTK) ?? email;
            var bayGio = DateTime.Now;
            if (bangHoSo == "KhachHang")
            {
                docHoSo.Root.Add(new XElement("KhachHang",
                    new XElement("MaKH", XmlHelper.SinhMa("KH", "KhachHang", "MaKH")),
                    new XElement("MaTK", maTK),
                    new XElement("HoTen", hoTen),
                    new XElement("Email", email),
                    new XElement("NgayTao", bayGio.ToString("yyyy-MM-ddTHH:mm:ss"))));
            }
            else
            {
                docHoSo.Root.Add(HoSoNhanVien(XmlHelper.SinhMa("NV", "NhanVien", "MaNV"), maTK, hoTen, null, ChucVuMacDinh(vaiTroMoi), bayGio));
            }
            return GhiHaiFile(docTK, docTKCu, bangHoSo, docHoSo, maTK);
        });
    }

    private static KetQua GhiHaiFile(XDocument docTK, XDocument docTKCu, string bangHoSo, XDocument docHoSo, string maTK)
    {
        var loi = XmlHelper.KiemTraXsd("TaiKhoan", docTK).Concat(XmlHelper.KiemTraXsd(bangHoSo, docHoSo)).ToList();
        if (loi.Count > 0)
        {
            return ThatBai(string.Join("; ", loi));
        }
        var kq = XmlHelper.Save("TaiKhoan", docTK);
        if (!kq.ThanhCong)
        {
            return kq;
        }
        kq = XmlHelper.Save(bangHoSo, docHoSo);
        if (!kq.ThanhCong)
        {
            XmlHelper.Save("TaiKhoan", docTKCu);
            return kq;
        }
        return new KetQua { ThanhCong = true, Ma = maTK };
    }

    private static XElement HoSoNhanVien(string maNV, string maTK, string hoTen, string? sdt, string chucVu, DateTime ngay) =>
        new("NhanVien",
            new XElement("MaNV", maNV),
            new XElement("MaTK", maTK),
            new XElement("HoTen", hoTen),
            sdt == null ? null : new XElement("SoDienThoai", sdt),
            new XElement("ChucVu", chucVu),
            new XElement("NgayVaoLam", ngay.ToString("yyyy-MM-dd")));

    private static string ChucVuMacDinh(string vaiTro) => vaiTro == VaiTro.Admin ? "Quản trị hệ thống" : "Tư vấn bán hàng";

    private static string? TimHoTen(string maTK) =>
        new[] { "NhanVien", "KhachHang" }
            .Select(bang => XmlHelper.Load(bang).Root!.Elements(bang).FirstOrDefault(e => (string?)e.Element("MaTK") == maTK))
            .Select(e => (string?)e?.Element("HoTen"))
            .FirstOrDefault(t => !string.IsNullOrEmpty(t));

    private static KetQua ThatBai(string thongBao) => new() { ThanhCong = false, ThongBao = thongBao };
}
