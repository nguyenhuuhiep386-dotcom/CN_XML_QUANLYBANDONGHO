using System.Text.Json;
using AurelMaison.Models;

namespace AurelMaison.Services;

public sealed class GioHangService(IDanhMucService danhMuc, IKhoService kho)
{
    private const string SessionKey = "AurelMaison.GioHang";
    public IReadOnlyList<MucGio> Lay(ISession session)
    {
        var json = session.GetString(SessionKey);
        return json is null ? [] : JsonSerializer.Deserialize<List<MucGio>>(json) ?? [];
    }

    public GioVm Xem(ISession session)
    {
        var models = danhMuc.LayDanhSach(new(null, null, null, null, null, null))
            .ToDictionary(x => x.MaMau, StringComparer.OrdinalIgnoreCase);
        return new(Lay(session).Select(line =>
        {
            var model = models.GetValueOrDefault(line.MaMau);
            return new DongGio(line.MaMau, model?.TenMau ?? line.MaMau, model?.HinhAnh ?? "",
                model?.Gia ?? 0, line.SoLuong, model?.SoConHang ?? 0, model is not null);
        }).ToList());
    }

    public KetQua Them(ISession session, string maMau, int soLuong)
    {
        if (soLuong <= 0) return CustomerResult.Loi("Số lượng phải lớn hơn 0.");
        var current = Lay(session).FirstOrDefault(m => m.MaMau.Equals(maMau, StringComparison.OrdinalIgnoreCase));
        long total = (long)(current?.SoLuong ?? 0) + soLuong;
        return total > int.MaxValue ? CustomerResult.Loi("Số lượng vượt giới hạn.") : CapNhat(session, maMau, (int)total);
    }

    public KetQua CapNhat(ISession session, string maMau, int soLuong)
    {
        var model = danhMuc.LayChiTiet(maMau);
        if (model is null || soLuong < 1 || soLuong > kho.DemConHang(model.MaMau))
            return CustomerResult.Loi("Sản phẩm không khả dụng hoặc số lượng vượt tồn kho.");
        var list = Lay(session).Where(m => m.MaMau != model.MaMau).ToList();
        list.Add(new(model.MaMau, soLuong));
        Save(session, list);
        return CustomerResult.Ok("Đã cập nhật giỏ hàng.");
    }

    public void Xoa(ISession session, string maMau) =>
        Save(session, Lay(session).Where(m => !m.MaMau.Equals(maMau, StringComparison.OrdinalIgnoreCase)).ToList());

    public decimal TongTien(ISession session) => Xem(session).TongTien;
    public bool KiemTraTonKho(ISession session) => Xem(session).HopLe;

    public void XoaCacMucDaDat(ISession session, IReadOnlyList<MucGio> dat)
    {
        var taken = dat.ToDictionary(x => x.MaMau, x => x.SoLuong);
        Save(session, Lay(session).Select(x => x with { SoLuong = x.SoLuong - taken.GetValueOrDefault(x.MaMau) })
            .Where(x => x.SoLuong > 0).ToList());
    }

    private static void Save(ISession session, IReadOnlyList<MucGio> list) =>
        session.SetString(SessionKey, JsonSerializer.Serialize(list));
}
