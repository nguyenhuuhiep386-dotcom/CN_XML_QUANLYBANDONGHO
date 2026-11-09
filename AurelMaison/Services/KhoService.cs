using AurelMaison.Models;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace AurelMaison.Services;

/// <summary>
/// Từng chiếc đồng hồ (ChiTietDongHo.xml) và phiếu nhập (PhieuNhap.xml).
/// Tồn khả dụng của một mẫu = số chiếc ConHang; không có cột số lượng riêng.
/// </summary>
public class KhoService
{
    private static readonly Regex SerialHopLe = new(@"^[A-Za-z0-9][A-Za-z0-9._\-]{0,29}$", RegexOptions.Compiled);

    // ===== Đọc =====

    public Dictionary<string, int> DemConHangTheoMau() =>
        DocChiec()
            .Where(c => c.TrangThai == TrangThaiChiec.ConHang)
            .GroupBy(c => c.MaMau)
            .ToDictionary(g => g.Key, g => g.Count());

    public int DemConHang(string maMau) => DemConHangTheoMau().GetValueOrDefault(maMau);

    /// <summary>Số chiếc theo từng trạng thái (ConHang, DaGiu, DaBan, KhongBan).</summary>
    public Dictionary<string, int> DemTheoTrangThai() =>
        DocChiec().GroupBy(c => c.TrangThai).ToDictionary(g => g.Key, g => g.Count());

    public List<string> LaySerialConHang(string maMau) =>
        DocChiec()
            .Where(c => c.MaMau == maMau && c.TrangThai == TrangThaiChiec.ConHang)
            .Select(c => c.Serial)
            .ToList();

    public List<ChiecDongHo> LayDanhSachChiec(string? maMau = null, string? trangThai = null, string? maPN = null)
    {
        var tenMau = XmlHelper.Load("DongHo").Root!.Elements("DongHo")
            .ToDictionary(e => (string?)e.Element("MaMau") ?? string.Empty, e => (string?)e.Element("TenMau") ?? string.Empty);

        return DocChiec()
            .Where(c => string.IsNullOrEmpty(maMau) || c.MaMau == maMau)
            .Where(c => string.IsNullOrEmpty(trangThai) || c.TrangThai == trangThai)
            .Where(c => string.IsNullOrEmpty(maPN) || c.MaPN == maPN)
            .Select(c => { c.TenMau = tenMau.GetValueOrDefault(c.MaMau, c.MaMau); return c; })
            .OrderBy(c => c.MaMau).ThenBy(c => c.Serial)
            .ToList();
    }

    public PhieuNhap? LayPhieuNhap(string? maPN)
    {
        if (string.IsNullOrEmpty(maPN))
        {
            return null;
        }
        var e = XmlHelper.TimTheoMa("PhieuNhap", "MaPN", maPN);
        return e == null ? null : new PhieuNhap
        {
            MaPN = maPN,
            NgayNhap = DateTime.TryParse((string?)e.Element("NgayNhap"), out var ngay) ? ngay : DateTime.MinValue,
            MaNV = (string?)e.Element("MaNV") ?? string.Empty,
            GhiChu = (string?)e.Element("GhiChu")
        };
    }

    // ===== Nhập kho =====

    /// <summary>
    /// Tạo một PhieuNhap và các ChiTietDongHo (ConHang) cho mẫu maMau trong cùng một lần khóa.
    /// Một serial sai hoặc trùng thì không nhập chiếc nào. Ma = MaPN.
    /// </summary>
    public KetQua NhapKho(string maMau, string maNV, string? ghiChu, IEnumerable<DongNhapKho> dong)
    {
        var dsDong = (dong ?? [])
            .Where(d => !string.IsNullOrWhiteSpace(d.Serial))
            .Select(d => new DongNhapKho
            {
                Serial = d.Serial!.Trim(),
                TinhTrang = d.TinhTrang,
                CoHop = d.CoHop,
                CoGiayTo = d.CoGiayTo
            })
            .ToList();
        ghiChu = string.IsNullOrWhiteSpace(ghiChu) ? null : ghiChu.Trim();

        if (dsDong.Count == 0)
        {
            return ThatBai("Nhập ít nhất một serial.");
        }
        if (ghiChu?.Length > 250)
        {
            return ThatBai("Ghi chú tối đa 250 ký tự.");
        }
        var serialSai = dsDong.FirstOrDefault(d => !SerialHopLe.IsMatch(d.Serial!));
        if (serialSai != null)
        {
            return ThatBai($"Serial \"{serialSai.Serial}\" không hợp lệ: 1–30 ký tự chữ, số, dấu chấm, gạch ngang hoặc gạch dưới.");
        }
        var tinhTrangSai = dsDong.FirstOrDefault(d => d.TinhTrang is not (TinhTrangChiec.Moi or TinhTrangChiec.DaSuDung));
        if (tinhTrangSai != null)
        {
            return ThatBai($"Tình trạng của serial {tinhTrangSai.Serial} không hợp lệ.");
        }
        var trongForm = dsDong.GroupBy(d => d.Serial!, StringComparer.OrdinalIgnoreCase).FirstOrDefault(g => g.Count() > 1);
        if (trongForm != null)
        {
            return ThatBai($"Serial {trongForm.Key} bị nhập hai lần trong phiếu.");
        }

        return XmlHelper.ChayTrongKhoa(() =>
        {
            var mau = XmlHelper.TimTheoMa("DongHo", "MaMau", maMau ?? string.Empty);
            if (mau == null)
            {
                return ThatBai($"Không tìm thấy mẫu {maMau}.");
            }
            if (XmlHelper.TimTheoMa("NhanVien", "MaNV", maNV ?? string.Empty) == null)
            {
                return ThatBai("Không xác định được nhân viên lập phiếu.");
            }

            var docCT = XmlHelper.Load("ChiTietDongHo");
            var daCo = docCT.Root!.Elements("ChiTietDongHo")
                .Select(e => (string?)e.Element("Serial") ?? string.Empty)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var trung = dsDong.Where(d => daCo.Contains(d.Serial!)).Select(d => d.Serial).ToList();
            if (trung.Count > 0)
            {
                return ThatBai($"Serial đã có trong kho: {string.Join(", ", trung)}. Không nhập chiếc nào.");
            }

            var docPN = XmlHelper.Load("PhieuNhap");
            var docPNCu = new XDocument(docPN);
            var maPN = XmlHelper.SinhMa("PN", "PhieuNhap", "MaPN");
            docPN.Root!.Add(new XElement("PhieuNhap",
                new XElement("MaPN", maPN),
                new XElement("NgayNhap", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss")),
                new XElement("MaNV", maNV),
                ghiChu == null ? null : new XElement("GhiChu", ghiChu)));

            foreach (var d in dsDong)
            {
                docCT.Root.Add(new XElement("ChiTietDongHo",
                    new XElement("Serial", d.Serial),
                    new XElement("MaMau", maMau),
                    new XElement("MaPN", maPN),
                    new XElement("TinhTrang", d.TinhTrang),
                    new XElement("CoHop", d.CoHop ? "true" : "false"),
                    new XElement("CoGiayTo", d.CoGiayTo ? "true" : "false"),
                    new XElement("TrangThai", TrangThaiChiec.ConHang)));
            }

            // Ghi hai file: kiểm tra cả hai trước, file sau lỗi thì trả lại file đầu.
            var loi = XmlHelper.KiemTraXsd("PhieuNhap", docPN).Concat(XmlHelper.KiemTraXsd("ChiTietDongHo", docCT)).ToList();
            if (loi.Count > 0)
            {
                return ThatBai(string.Join("; ", loi));
            }
            var kq = XmlHelper.Save("PhieuNhap", docPN);
            if (!kq.ThanhCong)
            {
                return kq;
            }
            kq = XmlHelper.Save("ChiTietDongHo", docCT);
            if (!kq.ThanhCong)
            {
                XmlHelper.Save("PhieuNhap", docPNCu);
                return kq;
            }
            return new KetQua { ThanhCong = true, Ma = maPN, ThongBao = $"Đã nhập {dsDong.Count} chiếc vào phiếu {maPN}." };
        });
    }

    // ===== Đổi trạng thái chiếc (dùng cho đơn hàng) =====

    /// <summary>ConHang → DaGiu. Một chiếc không hợp lệ thì không đổi chiếc nào.</summary>
    public KetQua GiuSerial(IEnumerable<string> dsSerial) =>
        ChuyenTrangThai(dsSerial, TrangThaiChiec.ConHang, TrangThaiChiec.DaGiu);

    /// <summary>DaGiu → ConHang (hủy đơn hoặc ghi đơn lỗi sau khi đã giữ).</summary>
    public KetQua GiaiPhongSerial(IEnumerable<string> dsSerial) =>
        ChuyenTrangThai(dsSerial, TrangThaiChiec.DaGiu, TrangThaiChiec.ConHang);

    /// <summary>DaGiu → DaBan (đơn hoàn tất).</summary>
    public KetQua DanhDauDaBan(IEnumerable<string> dsSerial) =>
        ChuyenTrangThai(dsSerial, TrangThaiChiec.DaGiu, TrangThaiChiec.DaBan);

    private static KetQua ChuyenTrangThai(IEnumerable<string> dsSerial, string tu, string den)
    {
        var ds = (dsSerial ?? []).ToList();
        if (ds.Count == 0)
        {
            return ThatBai("Danh sách serial trống.");
        }
        var lap = ds.GroupBy(s => s).FirstOrDefault(g => g.Count() > 1);
        if (lap != null)
        {
            return ThatBai($"Serial {lap.Key} bị lặp trong danh sách.");
        }

        return XmlHelper.ChayTrongKhoa(() =>
        {
            var doc = XmlHelper.Load("ChiTietDongHo");
            var theoSerial = doc.Root!.Elements("ChiTietDongHo")
                .ToDictionary(e => (string?)e.Element("Serial") ?? string.Empty);

            // Kiểm tra cả danh sách trước khi sửa chiếc nào.
            foreach (var s in ds)
            {
                if (!theoSerial.TryGetValue(s, out var e))
                {
                    return ThatBai($"Không tìm thấy serial {s}.");
                }
                var hienTai = (string?)e.Element("TrangThai");
                if (hienTai != tu)
                {
                    return ThatBai($"Serial {s} đang ở trạng thái {HienThi.TrangThaiChiec(hienTai)}, không chuyển được sang {HienThi.TrangThaiChiec(den)}.");
                }
            }
            foreach (var s in ds)
            {
                theoSerial[s].SetElementValue("TrangThai", den);
            }
            return XmlHelper.Save("ChiTietDongHo", doc);
        });
    }

    private static IEnumerable<ChiecDongHo> DocChiec() =>
        XmlHelper.Load("ChiTietDongHo").Root!.Elements("ChiTietDongHo").Select(ChiecDongHo.TuXml);

    private static KetQua ThatBai(string thongBao) => new() { ThanhCong = false, ThongBao = thongBao };
}
