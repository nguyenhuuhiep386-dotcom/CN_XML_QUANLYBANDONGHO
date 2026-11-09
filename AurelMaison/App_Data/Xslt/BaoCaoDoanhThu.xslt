<?xml version="1.0" encoding="utf-8"?>
<!--
  Báo cáo doanh thu bản in. Đầu vào: XML do BaoCaoService.TaoXml dựng
  <BaoCaoDoanhThu TuNgay NgayLap DenNgay> gồm TongHop, MauBanChay/Mau, DoanhSoNhanVien/NhanVien.
-->
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
  <xsl:output method="html" encoding="utf-8" indent="yes" doctype-system="about:legacy-compat"/>
  <xsl:decimal-format name="vn" decimal-separator="," grouping-separator="."/>

  <!-- 2026-11-30 → 30/11/2026 -->
  <xsl:template name="ngay">
    <xsl:param name="giaTri"/>
    <xsl:value-of select="concat(substring($giaTri, 9, 2), '/', substring($giaTri, 6, 2), '/', substring($giaTri, 1, 4))"/>
  </xsl:template>

  <xsl:template name="tien">
    <xsl:param name="giaTri"/>
    <xsl:value-of select="format-number($giaTri, '#.##0', 'vn')"/>
    <xsl:text> ₫</xsl:text>
  </xsl:template>

  <xsl:template match="/BaoCaoDoanhThu">
    <html lang="vi">
      <head>
        <meta charset="utf-8"/>
        <title>Báo cáo doanh thu</title>
        <style>
          body { font-family: Inter, Arial, sans-serif; color: #221e1b; margin: 40px; background: #fff; }
          h1 { font-family: 'Playfair Display', Georgia, serif; font-weight: 400; font-size: 30px; margin: 0 0 6px; }
          .thuong-hieu { letter-spacing: 3px; font-family: 'Playfair Display', Georgia, serif; color: #84673f; margin-bottom: 18px; }
          .phu { color: #6f7773; font-size: 13px; margin-bottom: 24px; }
          .chi-so { display: flex; gap: 16px; margin-bottom: 28px; }
          .chi-so div { flex: 1; border: 1px solid #e4e5df; padding: 14px 16px; }
          .chi-so span { display: block; color: #6f7773; font-size: 12px; }
          .chi-so strong { font-family: 'Playfair Display', Georgia, serif; font-weight: 400; font-size: 22px; }
          h2 { font-family: 'Playfair Display', Georgia, serif; font-weight: 400; font-size: 20px; margin: 24px 0 10px; }
          table { width: 100%; border-collapse: collapse; font-size: 13px; }
          th { background: #ede8df; text-align: left; font-weight: 400; padding: 10px 12px; }
          td { border-bottom: 1px solid #e4e5df; padding: 10px 12px; }
          td.so, th.so { text-align: right; }
          .trong { color: #6f7773; font-style: italic; }
          .nut-in { margin-top: 28px; background: #221e1b; color: #fff; border: 0; padding: 12px 20px; cursor: pointer; font-size: 13px; }
          @media print { .nut-in { display: none; } body { margin: 0; } }
        </style>
      </head>
      <body>
        <div class="thuong-hieu">AUREL MAISON</div>
        <h1>Báo cáo doanh thu</h1>
        <div class="phu">
          <xsl:text>Kỳ báo cáo: </xsl:text>
          <xsl:call-template name="ngay"><xsl:with-param name="giaTri" select="@TuNgay"/></xsl:call-template>
          <xsl:text> — </xsl:text>
          <xsl:call-template name="ngay"><xsl:with-param name="giaTri" select="@DenNgay"/></xsl:call-template>
          <xsl:text> · Lập ngày </xsl:text>
          <xsl:call-template name="ngay"><xsl:with-param name="giaTri" select="@NgayLap"/></xsl:call-template>
          <xsl:text> · Chỉ tính đơn hoàn tất, không tính đơn hủy.</xsl:text>
        </div>

        <div class="chi-so">
          <div><span>Doanh thu hoàn tất</span><strong><xsl:call-template name="tien"><xsl:with-param name="giaTri" select="TongHop/DoanhThu"/></xsl:call-template></strong></div>
          <div><span>Số chiếc đã bán</span><strong><xsl:value-of select="TongHop/SoChiec"/></strong></div>
          <div><span>Số đơn hoàn tất</span><strong><xsl:value-of select="TongHop/SoDon"/></strong></div>
          <div><span>Giá trị đơn trung bình</span><strong><xsl:call-template name="tien"><xsl:with-param name="giaTri" select="TongHop/GiaTriTrungBinh"/></xsl:call-template></strong></div>
        </div>

        <h2>Mẫu bán chạy</h2>
        <table>
          <tr><th>Mã mẫu</th><th>Tên mẫu</th><th class="so">Số chiếc</th><th class="so">Doanh thu</th></tr>
          <xsl:for-each select="MauBanChay/Mau">
            <tr>
              <td><xsl:value-of select="MaMau"/></td>
              <td><xsl:value-of select="TenMau"/></td>
              <td class="so"><xsl:value-of select="SoChiec"/></td>
              <td class="so"><xsl:call-template name="tien"><xsl:with-param name="giaTri" select="DoanhThu"/></xsl:call-template></td>
            </tr>
          </xsl:for-each>
          <xsl:if test="not(MauBanChay/Mau)">
            <tr><td colspan="4" class="trong">Không có đơn hoàn tất trong kỳ.</td></tr>
          </xsl:if>
        </table>

        <h2>Doanh số theo nhân viên phụ trách</h2>
        <table>
          <tr><th>Nhân viên</th><th class="so">Đơn hoàn tất</th><th class="so">Doanh thu</th><th class="so">Tỷ trọng</th></tr>
          <xsl:for-each select="DoanhSoNhanVien/NhanVien">
            <tr>
              <td>
                <xsl:value-of select="HoTen"/>
                <xsl:if test="MaNV != ''"> (<xsl:value-of select="MaNV"/>)</xsl:if>
              </td>
              <td class="so"><xsl:value-of select="SoDon"/></td>
              <td class="so"><xsl:call-template name="tien"><xsl:with-param name="giaTri" select="DoanhThu"/></xsl:call-template></td>
              <td class="so"><xsl:value-of select="format-number(TyTrong, '0,0', 'vn')"/>%</td>
            </tr>
          </xsl:for-each>
          <xsl:if test="not(DoanhSoNhanVien/NhanVien)">
            <tr><td colspan="4" class="trong">Không có đơn hoàn tất trong kỳ.</td></tr>
          </xsl:if>
        </table>

        <button type="button" class="nut-in" onclick="window.print()">In báo cáo</button>
      </body>
    </html>
  </xsl:template>
</xsl:stylesheet>
