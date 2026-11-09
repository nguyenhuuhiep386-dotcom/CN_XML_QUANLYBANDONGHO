namespace AurelMaison.Models;

/// <summary>Kết quả của mọi hàm ghi dữ liệu, thay cho việc ném lỗi ra giao diện.</summary>
public class KetQua
{
    public bool ThanhCong { get; set; }

    public string ThongBao { get; set; } = string.Empty;

    /// <summary>Mã vừa tạo hoặc vừa xử lý, ví dụ MaTK, MaDon, MaLH.</summary>
    public string Ma { get; set; } = string.Empty;
}

/// <summary>Kết quả tạo đơn: Ma là mã đơn, TongTien do máy chủ tính.</summary>
public class KetQuaTaoDon : KetQua
{
    public decimal TongTien { get; set; }
}
