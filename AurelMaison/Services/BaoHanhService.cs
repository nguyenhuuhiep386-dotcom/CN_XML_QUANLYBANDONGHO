using AurelMaison.Models;
using System.Xml.Linq;
using static AurelMaison.Services.DonHangService;

namespace AurelMaison.Services;

public class BaoHanhService(DonHangService donHang)
{
    public KetQua LapPhieu(string maDon, string serial, string maNV) => GhiAnToan(() =>
    {
        if (!NhanVienHopLe(maNV)) return new KetQua { ThongBao = "Không xác định được nhân viên." };
        var don = donHang.LayChiTiet(maDon);
        if (don == null || GiaTri(don, "TrangThai") != "HoanTat")
            return new KetQua { ThongBao = "Chỉ lập bảo hành cho đơn hoàn tất." };
        var ct = don.Element("ChiTiet")!.Elements().FirstOrDefault(x => GiaTri(x, "Serial") == serial && !string.IsNullOrWhiteSpace(serial));
        if (ct == null) return new KetQua { ThongBao = "Serial không thuộc đơn hàng." };
        var doc = XmlHelper.Load("PhieuBaoHanh");
        if (doc.Root!.Elements().Any(x => GiaTri(x, "MaDon") == maDon && GiaTri(x, "Serial") == serial))
            return new KetQua { ThongBao = "Chiếc đồng hồ đã có phiếu bảo hành cho đơn này." };
        var batDau = ((DateTime)don.Element("NgayHoanTat")!).Date;
        var hetHan = batDau.AddMonths((int)ct.Element("ThoiHanBaoHanh")!);
        var ma = XmlHelper.SinhMa("BH", "PhieuBaoHanh", "MaPBH", true);
        doc.Root.Add(new XElement("PhieuBaoHanh", new XElement("MaPBH", ma), new XElement("MaDon", maDon),
            new XElement("Serial", serial), new XElement("MaKH", GiaTri(don, "MaKH")),
            new XElement("NgayBatDau", batDau.ToString("yyyy-MM-dd")), new XElement("NgayHetHan", hetHan.ToString("yyyy-MM-dd")),
            new XElement("MaNVLap", maNV)));
        var kq = XmlHelper.Save("PhieuBaoHanh", doc);
        if (kq.ThanhCong) { kq.Ma = ma; kq.ThongBao = "Đã lập phiếu bảo hành."; }
        return kq;
    });

    public List<XElement> LayDanhSach() => XmlHelper.ChayTrongKhoa(() => XmlHelper.Load("PhieuBaoHanh")
        .Root!.Elements("PhieuBaoHanh").Select(x => new XElement(x)).ToList());
    public List<XElement> LayTheoKhach(string maKH) => LayDanhSach()
        .Where(x => !string.IsNullOrWhiteSpace(maKH) && GiaTri(x, "MaKH") == maKH).ToList();
    public XElement? LayPhieu(string ma) => XmlHelper.TimTheoMa("PhieuBaoHanh", "MaPBH", ma);

    // Tên cột lưu là MaPBH; hợp đồng XML công khai với Hiệp là MaPhieu.
    public XElement TaoPhanHoi(XElement phieu) => XmlHelper.ChayTrongKhoa(() =>
    {
        var don = donHang.LayChiTiet(GiaTri(phieu, "MaDon"));
        var ct = don?.Element("ChiTiet")?.Elements().FirstOrDefault(x => GiaTri(x, "Serial") == GiaTri(phieu, "Serial"));
        return new XElement("PhieuBaoHanh", new XElement("MaPhieu", GiaTri(phieu, "MaPBH")),
            new XElement("MaDon", GiaTri(phieu, "MaDon")), new XElement("TenMau", ct == null ? "" : GiaTri(ct, "TenMau")),
            new XElement("Serial", GiaTri(phieu, "Serial")), new XElement("NgayBatDau", GiaTri(phieu, "NgayBatDau")),
            new XElement("NgayHetHan", GiaTri(phieu, "NgayHetHan")),
            new XElement("TrangThai", DateTime.Today <= ((DateTime)phieu.Element("NgayHetHan")!).Date ? "ConBaoHanh" : "HetBaoHanh"));
    });
}
