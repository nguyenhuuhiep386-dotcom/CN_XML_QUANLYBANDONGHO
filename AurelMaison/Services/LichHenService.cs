using System.Xml.Linq;
using AurelMaison.Models;

namespace AurelMaison.Services;

public sealed class LichHenService(IDanhMucService danhMuc)
{
    public KetQua Tao(string maKH, DatLichForm form) => XmlHelper.ChayTrongKhoa(() =>
    {
        if (XmlHelper.Load("KhachHang").Find("MaKH", maKH) is null)
            return CustomerResult.Loi("Không tìm thấy hồ sơ khách hàng.");
        if (form.Showroom != Rules.Showroom || form.NgayHen is not { } time || time <= Rules.Now
            || time.TimeOfDay < TimeSpan.FromHours(9) || time.TimeOfDay > TimeSpan.FromHours(20))
            return CustomerResult.Loi("Chọn giờ hẹn trong tương lai, từ 09:00 đến 20:00 tại showroom.");
        var model = string.IsNullOrWhiteSpace(form.MaMau) ? null : danhMuc.LayChiTiet(form.MaMau);
        if (!string.IsNullOrWhiteSpace(form.MaMau) && model is null)
            return CustomerResult.Loi("Mẫu đồng hồ không khả dụng.");

        var doc = XmlHelper.Load("LichHen");
        string id = doc.Next("MaLH", "LH" + Rules.Now.Year, 4);
        doc.Root!.Add(new XElement("LichHen", new XElement("MaLH", id), new XElement("MaKH", maKH),
            model is null ? null : new XElement("MaMau", model.MaMau),
            new XElement("Showroom", form.Showroom), new XElement("NgayHen", XmlData.Iso(time)),
            new XElement("HoTen", form.HoTen.Trim()), new XElement("SoDienThoai", form.SoDienThoai.Trim()),
            string.IsNullOrWhiteSpace(form.GhiChu) ? null : new XElement("GhiChu", form.GhiChu.Trim()),
            new XElement("TrangThai", Rules.Pending), new XElement("NgayTao", XmlData.Iso(Rules.Now))));
        var saved = XmlHelper.Save("LichHen", doc);
        if (!saved.ThanhCong) return saved;
        return CustomerResult.Ok("Đã gửi lịch hẹn.", id);
    });

    public IReadOnlyList<LichHen> LayTheoKhach(string maKH) => XmlHelper.Load("LichHen").Rows().Select(Read)
        .Where(x => x.MaKH == maKH).OrderByDescending(x => x.NgayHen).ToList();

    public IReadOnlyList<LichHen> LayTheoNgay(DateOnly ngay, string? trangThai = null) =>
        XmlHelper.Load("LichHen").Rows().Select(Read).Where(x => DateOnly.FromDateTime(x.NgayHen) == ngay
            && (string.IsNullOrEmpty(trangThai) || x.TrangThai == trangThai)).OrderBy(x => x.NgayHen).ToList();

    public LichHen? LayChiTiet(string maLH)
    {
        var row = XmlHelper.Load("LichHen").Find("MaLH", maLH);
        return row is null ? null : Read(row);
    }

    public KetQua XacNhan(string maLH, string maNV) => Chuyen(maLH, maNV, Rules.Confirmed, null, Rules.Pending);
    public KetQua HoanTat(string maLH, string maNV) => Chuyen(maLH, maNV, Rules.Done, null, Rules.Confirmed);
    public KetQua Huy(string maLH, string maNV, string lyDo)
    {
        if (string.IsNullOrWhiteSpace(lyDo) || lyDo.Trim().Length > 250)
            return CustomerResult.Loi("Nhập lý do hủy từ 1 đến 250 ký tự.");
        return Chuyen(maLH, maNV, Rules.Cancelled, lyDo.Trim(), Rules.Pending, Rules.Confirmed);
    }

    private KetQua Chuyen(string id, string maNV, string next, string? reason, params string[] allowed) =>
        XmlHelper.ChayTrongKhoa(() =>
        {
            if (XmlHelper.Load("NhanVien").Find("MaNV", maNV) is null) return CustomerResult.Loi("Mã nhân viên không hợp lệ.");
            var doc = XmlHelper.Load("LichHen");
            var row = doc.Find("MaLH", id);
            if (row is null || !allowed.Contains(row.Value("TrangThai")))
                return CustomerResult.Loi("Lịch hẹn không tồn tại hoặc trạng thái không cho phép thao tác.");
            row.SetElementValue("TrangThai", next);
            row.SetOptional("MaNVXuLy", maNV, row.Element("LyDoHuy") is null ? "NgayTao" : "LyDoHuy");
            row.SetOptional("LyDoHuy", reason, "NgayTao");
            var saved = XmlHelper.Save("LichHen", doc);
            if (!saved.ThanhCong) return saved;
            return CustomerResult.Ok(Rules.State(next), row.Value("MaLH"));
        });

    private static LichHen Read(XElement x) => new(x.Value("MaLH"), x.Value("MaKH"), x.Optional("MaMau"),
        x.Value("Showroom"), (DateTime)x.Element("NgayHen")!, x.Value("HoTen"), x.Value("SoDienThoai"),
        x.Optional("GhiChu"), x.Value("TrangThai"), x.Optional("MaNVXuLy"), x.Optional("LyDoHuy"));
}
