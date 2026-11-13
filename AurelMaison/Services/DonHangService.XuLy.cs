using AurelMaison.Models;
using System.Xml.Linq;

namespace AurelMaison.Services;

public partial class DonHangService
{
    public List<XElement> LayDanhSach(string? tuKhoa = null, string? trangThai = null,
        string? nguonDon = null, DateTime? tuNgay = null, DateTime? denNgay = null) => XmlHelper.ChayTrongKhoa(() =>
        XmlHelper.Load("DonHang").Root!.Elements("DonHang")
            .Where(x => string.IsNullOrWhiteSpace(tuKhoa) || new[] { "MaDon", "TenNguoiNhan", "SoDienThoai" }
                .Any(c => GiaTri(x, c).Contains(tuKhoa.Trim(), StringComparison.OrdinalIgnoreCase)))
            .Where(x => string.IsNullOrEmpty(trangThai) || GiaTri(x, "TrangThai") == trangThai)
            .Where(x => string.IsNullOrEmpty(nguonDon) || GiaTri(x, "NguonDon") == nguonDon)
            .Where(x => tuNgay == null || ((DateTime)x.Element("NgayDat")!).Date >= tuNgay.Value.Date)
            .Where(x => denNgay == null || ((DateTime)x.Element("NgayDat")!).Date <= denNgay.Value.Date)
            .OrderByDescending(x => GiaTri(x, "NgayDat")).Select(x => new XElement(x)).ToList());

    // Chỉ gọi từ khu Staff đã kiểm tra quyền; khách dùng LayChiTietTheoKhach.
    public XElement? LayChiTiet(string maDon) => XmlHelper.ChayTrongKhoa(() =>
    {
        var don = XmlHelper.TimTheoMa("DonHang", "MaDon", maDon);
        if (don == null) return null;
        don = new XElement(don);
        don.Add(new XElement("ChiTiet", DocChiTiet(XmlHelper.Load("ChiTietDonHang"), maDon)));
        var hd = XmlHelper.Load("HoaDon").Root!.Elements("HoaDon").FirstOrDefault(x => GiaTri(x, "MaDon") == maDon);
        if (hd != null) don.Add(new XElement(hd));
        return don;
    });

    public KetQua XacNhan(string maDon, IEnumerable<string>? serial, string maNV) => GhiAnToan(() =>
    {
        if (!NhanVienHopLe(maNV)) return Loi("Không xác định được nhân viên.");
        var don = XmlHelper.Load("DonHang");
        var row = TimDon(don, maDon);
        if (row == null || GiaTri(row, "TrangThai") != "ChoXacNhan") return Loi("Chỉ xác nhận đơn đang chờ xác nhận.");
        var ct = XmlHelper.Load("ChiTietDonHang");
        var lines = DocChiTiet(ct, maDon);
        var ds = serial?.ToList() ?? [];
        if (lines.Count == 0 || ds.Count != lines.Count || ds.Distinct(StringComparer.Ordinal).Count() != ds.Count)
            return Loi("Chọn đủ serial, mỗi chiếc chỉ được chọn một lần.");
        // Thứ tự serial tương ứng STT, kiểm tra toàn bộ trước khi giữ bất kỳ chiếc nào.
        for (var i = 0; i < lines.Count; i++)
        {
            if (!kho.LaySerialConHang(GiaTri(lines[i], "MaMau")).Contains(ds[i]))
                return Loi($"Serial {ds[i]} không còn hàng hoặc không đúng mẫu ở dòng {i + 1}.");
            lines[i].SetElementValue("Serial", ds[i]);
        }
        row.SetElementValue("TrangThai", "DaXacNhan");
        row.SetElementValue("MaNVPhuTrach", maNV);
        return LuuBo(maDon, [("DonHang", don), ("ChiTietDonHang", ct)], () => kho.GiuSerial(ds));
    });

    public KetQua GiaoHang(string maDon, string maNV) => GhiAnToan(() =>
    {
        if (!NhanVienHopLe(maNV)) return Loi("Không xác định được nhân viên.");
        var doc = XmlHelper.Load("DonHang");
        var row = TimDon(doc, maDon);
        if (row == null || GiaTri(row, "TrangThai") != "DaXacNhan" || GiaTri(row, "HinhThucNhan") != "GiaoTanNoi")
            return Loi("Chỉ giao đơn đã xác nhận và nhận tận nơi.");
        row.SetElementValue("TrangThai", "DangGiao");
        return LuuBo(maDon, [("DonHang", doc)]);
    });

    public KetQua HoanTat(string maDon, string maNV, string phuongThucTT) => GhiAnToan(() =>
    {
        if (!NhanVienHopLe(maNV) || !ThanhToanHopLe(phuongThucTT)) return Loi("Nhân viên hoặc phương thức thanh toán không hợp lệ.");
        var doc = XmlHelper.Load("DonHang");
        var row = TimDon(doc, maDon);
        if (row == null || GiaTri(row, "TrangThai") is not ("DaXacNhan" or "DangGiao"))
            return Loi("Chỉ hoàn tất đơn đã xác nhận hoặc đang giao.");
        var lines = DocChiTiet(XmlHelper.Load("ChiTietDonHang"), maDon);
        var ds = lines.Select(x => GiaTri(x, "Serial")).ToList();
        if (ds.Count == 0 || ds.Any(string.IsNullOrWhiteSpace) || ds.Distinct().Count() != ds.Count)
            return Loi("Đơn chưa được gán đủ serial hợp lệ.");
        var khoGiu = kho.LayDanhSachChiec(trangThai: "DaGiu").ToDictionary(x => x.Serial);
        if (lines.Any(x => !khoGiu.TryGetValue(GiaTri(x, "Serial"), out var c) || c.MaMau != GiaTri(x, "MaMau")))
            return Loi("Serial của đơn không còn ở trạng thái đã giữ đúng mẫu.");
        var hd = XmlHelper.Load("HoaDon");
        if (hd.Root!.Elements().Any(x => GiaTri(x, "MaDon") == maDon)) return Loi("Đơn đã có hóa đơn.");
        var now = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
        row.SetElementValue("TrangThai", "HoanTat");
        row.SetElementValue("NgayHoanTat", now);
        row.SetElementValue("PhuongThucTT", phuongThucTT);
        hd.Root.Add(TaoHoaDon(maDon, maNV, (decimal)row.Element("TongTien")!, phuongThucTT, now));
        return LuuBo(maDon, [("DonHang", doc), ("HoaDon", hd)], () => kho.DanhDauDaBan(ds));
    });

    // Hủy của khách: chủ sở hữu lấy từ phiên, không có tùy chọn tự nâng quyền.
    public KetQua Huy(string maDon, string maKH, string lyDo) => HuyNoiBo(maDon, maKH, lyDo, false);
    public KetQua HuyBoiNhanVien(string maDon, string maNV, string lyDo) => HuyNoiBo(maDon, maNV, lyDo, true);

    private KetQua HuyNoiBo(string maDon, string nguoiHuy, string lyDo, bool nhanVien) => GhiAnToan(() =>
    {
        if (string.IsNullOrWhiteSpace(lyDo) || lyDo.Trim().Length > 250) return Loi("Nhập lý do hủy từ 1 đến 250 ký tự.");
        var doc = XmlHelper.Load("DonHang");
        var row = TimDon(doc, maDon);
        if (row == null) return Loi("Không tìm thấy đơn.");
        var state = GiaTri(row, "TrangThai");
        if (nhanVien ? !NhanVienHopLe(nguoiHuy) || state is not ("ChoXacNhan" or "DaXacNhan")
            : string.IsNullOrWhiteSpace(nguoiHuy) || GiaTri(row, "MaKH") != nguoiHuy || state != "ChoXacNhan")
            return Loi("Bạn không thể hủy đơn này ở trạng thái hiện tại.");
        var ds = DocChiTiet(XmlHelper.Load("ChiTietDonHang"), maDon).Select(x => GiaTri(x, "Serial")).ToList();
        row.SetElementValue("TrangThai", "DaHuy");
        row.SetElementValue("LyDoHuy", lyDo.Trim());
        return LuuBo(maDon, [("DonHang", doc)], state == "DaXacNhan" ? () => kho.GiaiPhongSerial(ds) : null);
    });

    public KetQuaTaoDon TaoDonTaiQuay(string maKH, IEnumerable<string>? serial, string maNV, string phuongThucTT) =>
        (KetQuaTaoDon)GhiAnToan(() =>
    {
        if (!NhanVienHopLe(maNV) || !ThanhToanHopLe(phuongThucTT)) return Loi("Nhân viên hoặc phương thức thanh toán không hợp lệ.");
        var kh = new KhachHangService().LayTheoMa(maKH);
        if (kh == null || string.IsNullOrWhiteSpace(kh.SoDienThoai)) return Loi("Chọn khách hàng có số điện thoại.");
        var ds = serial?.ToList() ?? [];
        if (ds.Count == 0 || ds.Distinct().Count() != ds.Count) return Loi("Chọn ít nhất một serial, không trùng lặp.");
        var chiec = kho.LayDanhSachChiec(trangThai: "ConHang").ToDictionary(x => x.Serial);
        var don = XmlHelper.Load("DonHang");
        var ct = XmlHelper.Load("ChiTietDonHang");
        var hd = XmlHelper.Load("HoaDon");
        var ma = XmlHelper.SinhMa("AM", "DonHang", "MaDon", true);
        decimal tong = 0;
        for (var i = 0; i < ds.Count; i++)
        {
            if (!chiec.TryGetValue(ds[i], out var c)) return Loi($"Serial {ds[i]} không còn hàng.");
            var mau = danhMuc.LayChiTiet(c.MaMau);
            if (mau == null || mau.TrangThai != "DangBan" || mau.Gia <= 0) return Loi("Mẫu không còn được bán.");
            tong += mau.Gia;
            ct.Root!.Add(new XElement("ChiTietDonHang", new XElement("MaDon", ma), new XElement("STT", i + 1),
                new XElement("MaMau", mau.MaMau), new XElement("TenMau", mau.TenMau), new XElement("Serial", ds[i]),
                new XElement("DonGia", mau.Gia), new XElement("ThoiHanBaoHanh", mau.ThoiHanBaoHanh)));
        }
        var now = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
        don.Root!.Add(new XElement("DonHang", new XElement("MaDon", ma), new XElement("MaKH", maKH),
            new XElement("NguonDon", "TaiQuay"), new XElement("MaNVLap", maNV), new XElement("MaNVPhuTrach", maNV),
            new XElement("NgayDat", now), new XElement("TenNguoiNhan", kh.HoTen), new XElement("SoDienThoai", kh.SoDienThoai),
            new XElement("HinhThucNhan", "NhanTaiShowroom"), new XElement("Showroom", "Tại quầy"),
            new XElement("TongTien", tong), new XElement("TrangThai", "HoanTat"), new XElement("NgayHoanTat", now), new XElement("PhuongThucTT", phuongThucTT)));
        hd.Root!.Add(TaoHoaDon(ma, maNV, tong, phuongThucTT, now));
        var kq = LuuBo(ma, [("DonHang", don), ("ChiTietDonHang", ct), ("HoaDon", hd)], () =>
        {
            var giu = kho.GiuSerial(ds);
            if (!giu.ThanhCong) return giu;
            var ban = kho.DanhDauDaBan(ds);
            if (!ban.ThanhCong)
            {
                var release = kho.GiaiPhongSerial(ds);
                if (!release.ThanhCong) ban.ThongBao += " Không giải phóng được serial: " + release.ThongBao;
            }
            return ban;
        });
        return new KetQuaTaoDon { ThanhCong = kq.ThanhCong, Ma = kq.Ma, ThongBao = kq.ThongBao, TongTien = kq.ThanhCong ? tong : 0 };
    });

    public string? XuatHoaDon(string maHD) => XmlHelper.ChayTrongKhoa(() =>
    {
        var hd = XmlHelper.TimTheoMa("HoaDon", "MaHD", maHD);
        if (hd == null) return null;
        var don = LayChiTiet(GiaTri(hd, "MaDon"));
        if (don == null) return null;
        return XmlHelper.XuatHtml(new XDocument(new XElement("PhieuHoaDon", new XElement(hd), don)), "PhieuHoaDon");
    });

    private static XElement TaoHoaDon(string ma, string nv, decimal tong, string tt, string now) => new("HoaDon",
        new XElement("MaHD", XmlHelper.SinhMa("HD", "HoaDon", "MaHD", true)), new XElement("MaDon", ma),
        new XElement("NgayLap", now), new XElement("MaNV", nv), new XElement("TongTien", tong), new XElement("PhuongThucTT", tt));
    internal static bool NhanVienHopLe(string ma) => !string.IsNullOrWhiteSpace(ma) && XmlHelper.TimTheoMa("NhanVien", "MaNV", ma) != null;
    private static bool ThanhToanHopLe(string tt) => tt is "TienMat" or "ChuyenKhoan" or "The";
    internal static string GiaTri(XElement e, string cot) => (string?)e.Element(cot) ?? "";
    private static XElement? TimDon(XDocument doc, string ma) => doc.Root!.Elements("DonHang").FirstOrDefault(x => GiaTri(x, "MaDon") == ma);
    private static List<XElement> DocChiTiet(XDocument doc, string ma) => doc.Root!.Elements("ChiTietDonHang")
        .Where(x => GiaTri(x, "MaDon") == ma).OrderBy(x => (int)x.Element("STT")!).ToList();

    internal static KetQua GhiAnToan(Func<KetQua> action) => XmlHelper.ChayTrongKhoa(() =>
    {
        try { return action(); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or System.Xml.XmlException
            or System.Xml.Schema.XmlSchemaException or OverflowException or FormatException or ArgumentOutOfRangeException)
        { return Loi("Không lưu được dữ liệu: " + e.Message); }
    });

    // Khóa bao trọn cả lần gọi. Kiểm tra mọi XSD trước khi ghi; phục hồi lỗi ghi thông thường.
    // Không bảo đảm rollback sau mất điện giữa nhiều file (ngoài phạm vi đề tài).
    private static KetQua LuuBo(string ma, (string Bang, XDocument Doc)[] bo, Func<KetQua>? doiKho = null)
    {
        var errors = bo.SelectMany(x => XmlHelper.KiemTraXsd(x.Bang, x.Doc)).ToList();
        if (errors.Count > 0) return Loi(string.Join("; ", errors));
        var cu = bo.Select(x => (x.Bang, Doc: XmlHelper.Load(x.Bang))).ToList();
        KetQua? fail = null;
        foreach (var x in bo)
        {
            var kq = XmlHelper.Save(x.Bang, x.Doc);
            if (!kq.ThanhCong) { fail = kq; break; }
        }
        if (fail == null && doiKho != null)
        {
            var kq = doiKho();
            if (!kq.ThanhCong) fail = kq;
        }
        if (fail == null) return new KetQua { ThanhCong = true, Ma = ma, ThongBao = "Đã lưu thành công." };
        foreach (var x in cu)
        {
            var restore = XmlHelper.Save(x.Bang, x.Doc);
            if (!restore.ThanhCong) fail.ThongBao += $" Không phục hồi được {x.Bang}: {restore.ThongBao}";
        }
        return fail;
    }
}
