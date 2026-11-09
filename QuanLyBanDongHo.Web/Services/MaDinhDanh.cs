namespace AurelMaison.Services;

/// <summary>Sinh mã kế tiếp từ các mã đang có trong file XML.</summary>
public static class MaDinhDanh
{
    /// <summary>Tiền tố + 3 số: TK001, KH001, NV001, HG001, DH001, PN001.</summary>
    public static string TaoMa(string tienTo, IEnumerable<string> maHienCo) =>
        $"{tienTo}{SoKeTiep(tienTo, maHienCo):D3}";

    /// <summary>Tiền tố + năm + 4 số: AM20260018, HD20260001, LH20260001, BH20260012.</summary>
    public static string TaoMaTheoNam(string tienTo, int nam, IEnumerable<string> maHienCo) =>
        $"{tienTo}{nam}{SoKeTiep($"{tienTo}{nam}", maHienCo):D4}";

    private static int SoKeTiep(string dauMa, IEnumerable<string> maHienCo) =>
        maHienCo
            .Where(ma => ma.StartsWith(dauMa, StringComparison.Ordinal))
            .Select(ma => int.TryParse(ma[dauMa.Length..], out var so) ? so : 0)
            .DefaultIfEmpty(0)
            .Max() + 1;
}
