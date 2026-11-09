using AurelMaison.Models;
using System.Xml.Linq;

namespace AurelMaison.Services;

/// <summary>Mẫu đồng hồ (DongHo.xml) và hãng (HangSanXuat.xml).</summary>
public class DanhMucService(KhoService khoService)
{
    // ===== Đọc =====

    public List<HangSanXuat> LayDanhSachHang()
    {
        var soMauTheoHang = XmlHelper.Load("DongHo").Root!.Elements("DongHo")
            .GroupBy(e => (string?)e.Element("MaHang") ?? string.Empty)
            .ToDictionary(g => g.Key, g => g.Count());

        return XmlHelper.Load("HangSanXuat").Root!.Elements("HangSanXuat")
            .Select(HangSanXuat.TuXml)
            .Select(h => { h.SoMau = soMauTheoHang.GetValueOrDefault(h.MaHang); return h; })
            .OrderBy(h => h.TenHang)
            .ToList();
    }

    /// <summary>Cho trang khách: chỉ mẫu DangBan, lọc theo các tiêu chí khác null.</summary>
    public List<MauDongHo> LayDanhSach(string? tuKhoa = null, string? maHang = null, decimal? giaTu = null,
        decimal? giaDen = null, string? loaiMay = null, string? chatLieu = null) =>
        DocTatCa()
            .Where(m => m.TrangThai == TrangThaiMau.DangBan)
            .Where(m => KhopTuKhoa(m, tuKhoa))
            .Where(m => string.IsNullOrEmpty(maHang) || m.MaHang == maHang)
            .Where(m => giaTu == null || m.Gia >= giaTu)
            .Where(m => giaDen == null || m.Gia <= giaDen)
            .Where(m => string.IsNullOrEmpty(loaiMay) || string.Equals(m.LoaiMay, loaiMay, StringComparison.OrdinalIgnoreCase))
            .Where(m => string.IsNullOrEmpty(chatLieu) || m.ChatLieu.Contains(chatLieu, StringComparison.OrdinalIgnoreCase))
            .ToList();

    /// <summary>Cho Admin: mọi trạng thái.</summary>
    public List<MauDongHo> LayDanhSachQuanTri(string? tuKhoa = null, string? maHang = null, string? trangThai = null) =>
        DocTatCa()
            .Where(m => KhopTuKhoa(m, tuKhoa))
            .Where(m => string.IsNullOrEmpty(maHang) || m.MaHang == maHang)
            .Where(m => string.IsNullOrEmpty(trangThai) || m.TrangThai == trangThai)
            .ToList();

    public MauDongHo? LayChiTiet(string? maMau)
    {
        if (string.IsNullOrEmpty(maMau))
        {
            return null;
        }
        return DocTatCa().FirstOrDefault(m => m.MaMau == maMau);
    }

    public string SinhMaMauMoi() => XmlHelper.ChayTrongKhoa(() => XmlHelper.SinhMa("DH", "DongHo", "MaMau"));

    // ===== Ghi mẫu =====

    /// <summary>
    /// laMoi = true: thêm (MaMau rỗng thì tự sinh). laMoi = false: sửa mẫu MaMau, giữ nguyên trạng thái.
    /// Ma = MaMau.
    /// </summary>
    public KetQua LuuMau(MauDongHo mau, bool laMoi)
    {
        mau.MaMau = (mau.MaMau ?? string.Empty).Trim().ToUpperInvariant();
        mau.TenMau = (mau.TenMau ?? string.Empty).Trim();
        mau.LoaiMay = (mau.LoaiMay ?? string.Empty).Trim();
        mau.ChatLieu = (mau.ChatLieu ?? string.Empty).Trim();
        mau.MoTa = string.IsNullOrWhiteSpace(mau.MoTa) ? null : mau.MoTa.Trim();
        mau.HinhAnh = string.IsNullOrWhiteSpace(mau.HinhAnh) ? null : mau.HinhAnh.Trim();

        var loi = KiemTraMau(mau);
        if (loi != null)
        {
            return ThatBai(loi);
        }

        return XmlHelper.ChayTrongKhoa(() =>
        {
            if (XmlHelper.TimTheoMa("HangSanXuat", "MaHang", mau.MaHang) == null)
            {
                return ThatBai($"Hãng {mau.MaHang} không tồn tại.");
            }

            if (laMoi)
            {
                if (string.IsNullOrEmpty(mau.MaMau))
                {
                    mau.MaMau = XmlHelper.SinhMa("DH", "DongHo", "MaMau");
                }
                else if (!System.Text.RegularExpressions.Regex.IsMatch(mau.MaMau, @"^DH\d{3,}$"))
                {
                    return ThatBai("Mã mẫu phải có dạng DH + 3 chữ số, ví dụ DH017.");
                }
                mau.TrangThai = TrangThaiMau.DangBan;
                return XmlHelper.Them("DongHo", "MaMau", mau.ToXml());
            }

            var cu = XmlHelper.TimTheoMa("DongHo", "MaMau", mau.MaMau);
            if (cu == null)
            {
                return ThatBai($"Không tìm thấy mẫu {mau.MaMau}.");
            }
            mau.TrangThai = (string?)cu.Element("TrangThai") ?? TrangThaiMau.DangBan;
            var moi = mau.ToXml();
            return XmlHelper.Sua("DongHo", "MaMau", mau.MaMau, dong => dong.ReplaceNodes(moi.Nodes()));
        });
    }

    /// <summary>Ngừng bán / mở bán lại. Mẫu ngừng bán vẫn giữ lịch sử đơn và serial.</summary>
    public KetQua DoiTrangThaiMau(string maMau, string trangThai)
    {
        if (trangThai is not (TrangThaiMau.DangBan or TrangThaiMau.NgungBan))
        {
            return ThatBai("Trạng thái không hợp lệ.");
        }
        return XmlHelper.Sua("DongHo", "MaMau", maMau, dong => dong.SetElementValue("TrangThai", trangThai));
    }

    // ===== Ghi hãng =====

    public KetQua ThemHang(string tenHang, string? quocGia, string? moTa)
    {
        var loi = KiemTraHang(ref tenHang, ref quocGia, ref moTa);
        if (loi != null)
        {
            return ThatBai(loi);
        }
        var ten = tenHang;
        var qg = quocGia;
        var mt = moTa;

        return XmlHelper.ChayTrongKhoa(() =>
        {
            if (TrungTenHang(ten, null))
            {
                return ThatBai($"Hãng \"{ten}\" đã tồn tại.");
            }
            var ma = XmlHelper.SinhMa("HG", "HangSanXuat", "MaHang");
            return XmlHelper.Them("HangSanXuat", "MaHang", new XElement("HangSanXuat",
                new XElement("MaHang", ma),
                new XElement("TenHang", ten),
                qg == null ? null : new XElement("QuocGia", qg),
                mt == null ? null : new XElement("MoTa", mt)));
        });
    }

    public KetQua SuaHang(string maHang, string tenHang, string? quocGia, string? moTa)
    {
        var loi = KiemTraHang(ref tenHang, ref quocGia, ref moTa);
        if (loi != null)
        {
            return ThatBai(loi);
        }
        var ten = tenHang;
        var qg = quocGia;
        var mt = moTa;

        return XmlHelper.ChayTrongKhoa(() =>
        {
            if (TrungTenHang(ten, maHang))
            {
                return ThatBai($"Hãng \"{ten}\" đã tồn tại.");
            }
            return XmlHelper.Sua("HangSanXuat", "MaHang", maHang, dong =>
            {
                dong.SetElementValue("TenHang", ten);
                dong.SetElementValue("QuocGia", qg);
                dong.SetElementValue("MoTa", mt);
            });
        });
    }

    /// <summary>Hãng đã có mẫu (kể cả mẫu ngừng bán) thì không xóa được.</summary>
    public KetQua XoaHang(string maHang) => XmlHelper.ChayTrongKhoa(() =>
    {
        var soMau = XmlHelper.Load("DongHo").Root!.Elements("DongHo").Count(e => (string?)e.Element("MaHang") == maHang);
        if (soMau > 0)
        {
            return ThatBai($"Hãng {maHang} đang có {soMau} mẫu đồng hồ, không thể xóa.");
        }
        return XmlHelper.Xoa("HangSanXuat", "MaHang", maHang);
    });

    // ===== Nội bộ =====

    private List<MauDongHo> DocTatCa()
    {
        var tenHang = XmlHelper.Load("HangSanXuat").Root!.Elements("HangSanXuat")
            .ToDictionary(e => (string?)e.Element("MaHang") ?? string.Empty, e => (string?)e.Element("TenHang") ?? string.Empty);
        var conHang = khoService.DemConHangTheoMau();

        return XmlHelper.Load("DongHo").Root!.Elements("DongHo")
            .Select(MauDongHo.TuXml)
            .Select(m =>
            {
                m.TenHang = tenHang.GetValueOrDefault(m.MaHang, m.MaHang);
                m.SoConHang = conHang.GetValueOrDefault(m.MaMau);
                return m;
            })
            .OrderBy(m => m.MaMau)
            .ToList();
    }

    private static bool KhopTuKhoa(MauDongHo m, string? tuKhoa) =>
        string.IsNullOrWhiteSpace(tuKhoa)
        || m.MaMau.Contains(tuKhoa.Trim(), StringComparison.OrdinalIgnoreCase)
        || m.TenMau.Contains(tuKhoa.Trim(), StringComparison.OrdinalIgnoreCase)
        || m.TenHang.Contains(tuKhoa.Trim(), StringComparison.OrdinalIgnoreCase);

    private static bool TrungTenHang(string ten, string? boQuaMa) =>
        XmlHelper.Load("HangSanXuat").Root!.Elements("HangSanXuat").Any(e =>
            (string?)e.Element("MaHang") != boQuaMa
            && string.Equals((string?)e.Element("TenHang"), ten, StringComparison.OrdinalIgnoreCase));

    private static string? KiemTraMau(MauDongHo m)
    {
        if (m.TenMau.Length is 0 or > 150) return "Tên mẫu không được trống và tối đa 150 ký tự.";
        if (string.IsNullOrEmpty(m.MaHang)) return "Chưa chọn hãng.";
        if (m.Gia <= 0) return "Giá phải lớn hơn 0.";
        if (m.Gia != decimal.Truncate(m.Gia)) return "Giá phải là số nguyên (đồng).";
        if (m.LoaiMay.Length is 0 or > 50) return "Loại máy không được trống và tối đa 50 ký tự.";
        if (m.KichThuoc <= 0 || m.KichThuoc > 999.9m) return "Kích thước phải lớn hơn 0 và nhỏ hơn 1000 mm.";
        if (m.KichThuoc != decimal.Round(m.KichThuoc, 1)) return "Kích thước chỉ lấy 1 chữ số thập phân.";
        if (m.ChatLieu.Length is 0 or > 100) return "Chất liệu không được trống và tối đa 100 ký tự.";
        if (m.MoTa?.Length > 1000) return "Mô tả tối đa 1000 ký tự.";
        if (m.ThoiHanBaoHanh is < 0 or > 120) return "Thời hạn bảo hành phải từ 0 đến 120 tháng.";
        return null;
    }

    private static string? KiemTraHang(ref string tenHang, ref string? quocGia, ref string? moTa)
    {
        tenHang = (tenHang ?? string.Empty).Trim();
        quocGia = string.IsNullOrWhiteSpace(quocGia) ? null : quocGia.Trim();
        moTa = string.IsNullOrWhiteSpace(moTa) ? null : moTa.Trim();
        if (tenHang.Length is 0 or > 100) return "Tên hãng không được trống và tối đa 100 ký tự.";
        if (quocGia?.Length > 50) return "Quốc gia tối đa 50 ký tự.";
        if (moTa?.Length > 500) return "Mô tả hãng tối đa 500 ký tự.";
        return null;
    }

    private static KetQua ThatBai(string thongBao) => new() { ThanhCong = false, ThongBao = thongBao };
}
