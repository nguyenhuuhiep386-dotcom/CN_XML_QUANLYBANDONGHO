<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
  <xsl:output method="html" encoding="utf-8" />
  <xsl:template match="/PhieuHoaDon">
    <html lang="vi"><head><meta charset="utf-8"/><title>Hóa đơn <xsl:value-of select="HoaDon/MaHD"/></title>
      <style>body{font:16px Arial,sans-serif;color:#252525;max-width:900px;margin:48px auto;padding:0 24px}h1{font-size:28px}table{width:100%;border-collapse:collapse;margin:24px 0}th,td{padding:12px;text-align:left;border-bottom:1px solid #ddd}.total{text-align:right;font-size:22px}.actions{display:flex;gap:20px}button{padding:10px 24px;cursor:pointer}@media print{.actions{display:none}body{margin:0}}</style>
    </head><body>
      <p>AUREL MAISON</p><h1>Hóa đơn bán hàng</h1>
      <p>Mã hóa đơn: <strong><xsl:value-of select="HoaDon/MaHD"/></strong> · Đơn hàng: <xsl:value-of select="DonHang/MaDon"/></p>
      <p>Ngày lập: <xsl:value-of select="HoaDon/NgayLap"/> · Nhân viên hoàn tất: <xsl:value-of select="HoaDon/MaNV"/></p>
      <p>Khách nhận: <xsl:value-of select="DonHang/TenNguoiNhan"/> · <xsl:value-of select="DonHang/SoDienThoai"/></p>
      <p>Nơi nhận: <xsl:value-of select="DonHang/DiaChiNhan | DonHang/Showroom"/></p>
      <table><thead><tr><th>STT</th><th>Mẫu đồng hồ</th><th>Serial</th><th>Bảo hành</th><th>Đơn giá (VNĐ)</th></tr></thead><tbody>
        <xsl:for-each select="DonHang/ChiTiet/ChiTietDonHang"><tr>
          <td><xsl:value-of select="STT"/></td><td><xsl:value-of select="TenMau"/></td><td><xsl:value-of select="Serial"/></td>
          <td><xsl:value-of select="ThoiHanBaoHanh"/> tháng</td><td><xsl:value-of select="format-number(DonGia,'#,##0')"/></td>
        </tr></xsl:for-each>
      </tbody></table>
      <p class="total">Tổng thanh toán: <strong><xsl:value-of select="format-number(HoaDon/TongTien,'#,##0')"/> VNĐ</strong></p>
      <p>Phương thức: <xsl:choose><xsl:when test="HoaDon/PhuongThucTT='TienMat'">Tiền mặt</xsl:when><xsl:when test="HoaDon/PhuongThucTT='The'">Thẻ</xsl:when><xsl:otherwise>Chuyển khoản</xsl:otherwise></xsl:choose></p>
      <div class="actions"><button type="button" onclick="window.print()">In hóa đơn</button><a href="/Staff/DonHang">Về danh sách đơn</a></div>
    </body></html>
  </xsl:template>
</xsl:stylesheet>
