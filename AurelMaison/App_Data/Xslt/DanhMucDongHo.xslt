<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
  <xsl:output method="html" encoding="utf-8" indent="yes" />
  <xsl:decimal-format name="vn" decimal-separator="," grouping-separator="." />
  <xsl:template match="/DanhSachDongHo">
    <html lang="vi">
      <head>
        <meta charset="utf-8" />
        <meta name="viewport" content="width=device-width, initial-scale=1" />
        <title>Danh mục đồng hồ | Aurel Maison</title>
        <style>
          body{font:16px system-ui,sans-serif;margin:32px;color:#1c3028;background:#fbfaf5}
          main{max-width:1100px;margin:auto}table{width:100%;border-collapse:collapse}
          td,th{padding:12px;border-bottom:1px solid #dce2db;text-align:left}
          th{background:#203a31;color:white}td:last-child{text-align:right}a{color:#275342}
        </style>
      </head>
      <body>
        <main>
          <p>AUREL MAISON · DANH MỤC XML</p>
          <h1>Danh mục đồng hồ</h1>
          <p><a href="/DanhMuc">Về bộ sưu tập</a></p>
          <table>
            <thead><tr><th>Mã mẫu</th><th>Tên đồng hồ</th><th>Loại máy</th><th>Chất liệu</th><th>Bảo hành</th><th>Giá</th></tr></thead>
            <tbody>
              <xsl:for-each select="DongHo[TrangThai='DangBan']">
                <xsl:sort select="MaMau" />
                <tr>
                  <td><xsl:value-of select="MaMau" /></td>
                  <td><xsl:value-of select="TenMau" /></td>
                  <td><xsl:value-of select="LoaiMay" /></td>
                  <td><xsl:value-of select="ChatLieu" /></td>
                  <td><xsl:value-of select="ThoiHanBaoHanh" /> tháng</td>
                  <td><xsl:value-of select="format-number(Gia, '#.##0', 'vn')" /> ₫</td>
                </tr>
              </xsl:for-each>
            </tbody>
          </table>
        </main>
      </body>
    </html>
  </xsl:template>
</xsl:stylesheet>
