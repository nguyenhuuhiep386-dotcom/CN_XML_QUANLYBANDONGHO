using AurelMaison.Models;
using System.Xml.Linq;

namespace AurelMaison.Services;

/// <summary>Nối hợp đồng giao diện khách với dịch vụ đơn hàng dùng chung của Nguyên.</summary>
public sealed class CustomerOrderAdapter(DonHangService orders) : IDonHangService
{
    public KetQuaTaoDon TaoDonOnline(string maKH, IReadOnlyList<MucGio> gio, DatHangForm form) =>
        orders.TaoDonOnline(maKH, gio.Select(x => new MucDatHang(x.MaMau, x.SoLuong)),
            new ThongTinNhanHang(form.TenNguoiNhan, form.SoDienThoai, form.HinhThucNhan, form.DiaChiNhan, form.Showroom));

    public IReadOnlyList<DonHang> LayTheoKhach(string maKH) => XmlHelper.ChayTrongKhoa(() =>
        orders.LayTheoKhach(maKH).Select(x => Read(orders.LayChiTietTheoKhach(x.Value("MaDon"), maKH)!)).ToList());

    public DonHang? LayChiTiet(string maDon, string maKH)
    {
        var row = orders.LayChiTietTheoKhach(maDon, maKH);
        return row == null ? null : Read(row);
    }

    public KetQua Huy(string maDon, string maKH, string lyDo) => orders.Huy(maDon, maKH, lyDo);

    private static DonHang Read(XElement x) => new(x.Value("MaDon"), x.Value("MaKH"), (DateTime)x.Element("NgayDat")!,
        x.Value("TrangThai"), x.Money("TongTien"), x.Value("TenNguoiNhan"), x.Value("SoDienThoai"), x.Value("HinhThucNhan"),
        x.Optional("DiaChiNhan"), x.Optional("Showroom"), x.Optional("LyDoHuy"),
        x.Element("ChiTiet")!.Elements().Select(row => new ChiTietDon(row.Value("MaMau"), row.Value("TenMau"), row.Money("DonGia"))).ToList());
}
