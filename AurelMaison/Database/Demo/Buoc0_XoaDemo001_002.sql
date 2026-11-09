/* =====================================================================
   BƯỚC 0 CỦA BUỔI DEMO — đưa dữ liệu về trạng thái "DH001 chưa có hàng"
   1. Chạy file này trong SSMS (sau khi đã chạy QuanLyBanDongHo.sql).
   2. Vào web bằng admin@aurel.vn → Dữ liệu XML → tick xác nhận → "Khởi tạo từ SQL".
   3. Bước 1 của demo: Admin vào Serial & nhập kho, nhập DEMO001, DEMO002 cho DH001;
      trang khách hiện "Còn 2", dropdown serial của nhân viên có đủ 2 chiếc.
   Chỉ xóa khi hai serial chưa nằm trong đơn hàng / phiếu bảo hành nào.
   ===================================================================== */
USE QuanLyBanDongHo;
GO
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF EXISTS (SELECT 1 FROM dbo.ChiTietDonHang WHERE Serial IN ('DEMO001', 'DEMO002'))
   OR EXISTS (SELECT 1 FROM dbo.PhieuBaoHanh WHERE Serial IN ('DEMO001', 'DEMO002'))
BEGIN
    ROLLBACK TRANSACTION;
    RAISERROR(N'DEMO001/DEMO002 đã nằm trong đơn hoặc phiếu bảo hành. Chạy lại QuanLyBanDongHo.sql trước.', 16, 1);
    RETURN;
END

DELETE FROM dbo.ChiTietDongHo WHERE Serial IN ('DEMO001', 'DEMO002');

-- PN002 chỉ chứa hai chiếc trên; xóa luôn khi đã trống để lần nhập trên web lại thành PN002.
DELETE FROM dbo.PhieuNhap
WHERE MaPN = 'PN002' AND NOT EXISTS (SELECT 1 FROM dbo.ChiTietDongHo WHERE MaPN = 'PN002');

COMMIT TRANSACTION;
GO

SELECT MaMau, TenMau, SoConHang FROM dbo.vw_TonKhoTheoMau WHERE MaMau = 'DH001';   -- mong đợi SoConHang = 0
GO
