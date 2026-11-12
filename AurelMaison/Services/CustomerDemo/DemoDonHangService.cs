using System.Xml.Linq;
using AurelMaison.Models;

namespace AurelMaison.Services.CustomerDemo;

// Standalone demo for the customer flows. Nguyên owns the integrated order workflow.
public sealed class DemoDonHangService(IDanhMucService danhMuc) : IDonHangService
{
    public KetQuaTaoDon TaoDonOnline(string maKH, IReadOnlyList<MucGio> gio, DatHangForm form) =>
        XmlHelper.ChayTrongKhoa<KetQuaTaoDon>(() =>
        {
            if (gio.Count == 0 || XmlHelper.Load("KhachHang").Find("MaKH", maKH) is null)
                return Loi("Giỏ hàng trống hoặc hồ sơ khách không hợp lệ.");
            var selected = new List<(DongHo Model, int Quantity)>();
            foreach (var line in gio.GroupBy(x => x.MaMau, StringComparer.OrdinalIgnoreCase))
            {
                var model = danhMuc.LayChiTiet(line.Key);
                long quantity = line.Sum(x => (long)x.SoLuong);
                if (model is null || line.Any(x => x.SoLuong < 1) || quantity > model.SoConHang)
                    return Loi("Tồn kho vừa thay đổi. Hãy kiểm tra lại giỏ hàng.");
                selected.Add((model, (int)quantity));
            }

            var orders = XmlHelper.Load("DonHang");
            var details = XmlHelper.Load("ChiTietDonHang");
            string id = orders.Next("MaDon", "AM" + Rules.Now.Year, 4);
            decimal total = selected.Sum(x => x.Model.Gia * x.Quantity);
            if (total > 999999999999999999m) return Loi("Tổng tiền vượt giới hạn.");
            orders.Root!.Add(new XElement("DonHang", new XElement("MaDon", id), new XElement("MaKH", maKH),
                new XElement("NguonDon", "Online"), new XElement("NgayDat", XmlData.Iso(Rules.Now)),
                new XElement("TenNguoiNhan", form.TenNguoiNhan.Trim()), new XElement("SoDienThoai", form.SoDienThoai.Trim()),
                new XElement("HinhThucNhan", form.HinhThucNhan),
                form.HinhThucNhan == Rules.Delivery ? new XElement("DiaChiNhan", form.DiaChiNhan!.Trim()) : new XElement("Showroom", form.Showroom),
                new XElement("TongTien", total), new XElement("TrangThai", Rules.Pending)));

            int index = 0;
            foreach (var (model, quantity) in selected)
                for (int i = 0; i < quantity; i++)
                    details.Root!.Add(new XElement("ChiTietDonHang", new XElement("MaDon", id),
                        new XElement("STT", ++index), new XElement("MaMau", model.MaMau),
                        new XElement("TenMau", model.TenMau), new XElement("DonGia", model.Gia),
                        new XElement("ThoiHanBaoHanh", model.ThoiHanBaoHanh)));
            var saved = DemoXmlWrites.GhiNhieu(("DonHang", orders), ("ChiTietDonHang", details));
            if (!saved.ThanhCong) return new() { ThongBao = saved.ThongBao };
            return new() { ThanhCong = true, ThongBao = "Đặt hàng thành công.", Ma = id, TongTien = total };
        });

    public IReadOnlyList<DonHang> LayTheoKhach(string maKH) => XmlHelper.ChayTrongKhoa(() =>
    {
        var details = XmlHelper.Load("ChiTietDonHang").Rows().ToLookup(x => x.Value("MaDon"));
        return XmlHelper.Load("DonHang").Rows().Where(x => x.Value("MaKH") == maKH)
            .Select(x => Read(x, details[x.Value("MaDon")])).OrderByDescending(x => x.NgayDat).ToList();
    });

    public DonHang? LayChiTiet(string maDon, string maKH) => LayTheoKhach(maKH)
        .FirstOrDefault(x => x.MaDon.Equals(maDon, StringComparison.OrdinalIgnoreCase));

    public KetQua Huy(string maDon, string maKH, string lyDo) => XmlHelper.ChayTrongKhoa(() =>
    {
        if (string.IsNullOrWhiteSpace(lyDo) || lyDo.Trim().Length > 250) return CustomerResult.Loi("Nhập lý do hủy hợp lệ.");
        var doc = XmlHelper.Load("DonHang");
        var row = doc.Find("MaDon", maDon);
        if (row is null || row.Value("MaKH") != maKH || row.Value("TrangThai") != Rules.Pending)
            return CustomerResult.Loi("Chỉ hủy được đơn của bạn đang chờ xác nhận.");
        row.SetElementValue("TrangThai", Rules.Cancelled);
        row.SetElementValue("LyDoHuy", lyDo.Trim());
        var saved = XmlHelper.Save("DonHang", doc);
        if (!saved.ThanhCong) return saved;
        return CustomerResult.Ok("Đã hủy đơn hàng.", row.Value("MaDon"));
    });

    private static KetQuaTaoDon Loi(string message) => new() { ThongBao = message };

    private static DonHang Read(XElement x, IEnumerable<XElement> details) =>
        new(x.Value("MaDon"), x.Value("MaKH"), (DateTime)x.Element("NgayDat")!, x.Value("TrangThai"), x.Money("TongTien"),
            x.Value("TenNguoiNhan"), x.Value("SoDienThoai"), x.Value("HinhThucNhan"), x.Optional("DiaChiNhan"),
            x.Optional("Showroom"), x.Optional("LyDoHuy"), details.OrderBy(row => (int)row.Element("STT")!)
                .Select(row => new ChiTietDon(row.Value("MaMau"), row.Value("TenMau"), row.Money("DonGia"))).ToList());
}
