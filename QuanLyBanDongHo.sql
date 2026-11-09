/* =====================================================================
   QUẢN LÝ BÁN ĐỒNG HỒ — AUREL MAISON
   Bài tập lớn môn Công nghệ XML — Nhóm: Hiệp, Nguyên, Phương
   Script SQL Server: tạo CSDL, 12 bảng, ràng buộc, dữ liệu mẫu, view
   báo cáo và truy vấn FOR XML để xuất đúng cấu trúc file XML của nhóm.

   Quy ước:
   - Tên bảng = tên file XML (DongHo  -> App_Data/Xml/DongHo.xml)
   - Mã trạng thái lưu không dấu, giao diện hiển thị tiếng Việt
   - Tiền: DECIMAL(18,0) (VNĐ) ; Ngày giờ: DATETIME2(0)
   - Mật khẩu: SHA256(matKhau + Salt) dạng hex chữ thường
     Tất cả tài khoản mẫu có mật khẩu: 123456

   Chủ sở hữu bảng (người viết XSD + dịch vụ):
   - Phương : TaiKhoan, KhachHang, NhanVien, HangSanXuat, DongHo,
              ChiTietDongHo, PhieuNhap
   - Hiệp   : LichHen
   - Nguyên : DonHang, ChiTietDonHang, HoaDon, PhieuBaoHanh
   ===================================================================== */

IF DB_ID(N'QuanLyBanDongHo') IS NULL
    CREATE DATABASE QuanLyBanDongHo;
GO
USE QuanLyBanDongHo;
GO

/* ---------- Xóa bảng cũ (thứ tự ngược khóa ngoại) ---------- */
IF OBJECT_ID(N'dbo.vw_DoanhThuHoanTat', N'V') IS NOT NULL DROP VIEW dbo.vw_DoanhThuHoanTat;
IF OBJECT_ID(N'dbo.vw_TonKhoTheoMau',   N'V') IS NOT NULL DROP VIEW dbo.vw_TonKhoTheoMau;
IF OBJECT_ID(N'dbo.PhieuBaoHanh')   IS NOT NULL DROP TABLE dbo.PhieuBaoHanh;
IF OBJECT_ID(N'dbo.HoaDon')         IS NOT NULL DROP TABLE dbo.HoaDon;
IF OBJECT_ID(N'dbo.ChiTietDonHang') IS NOT NULL DROP TABLE dbo.ChiTietDonHang;
IF OBJECT_ID(N'dbo.DonHang')        IS NOT NULL DROP TABLE dbo.DonHang;
IF OBJECT_ID(N'dbo.LichHen')        IS NOT NULL DROP TABLE dbo.LichHen;
IF OBJECT_ID(N'dbo.ChiTietDongHo')  IS NOT NULL DROP TABLE dbo.ChiTietDongHo;
IF OBJECT_ID(N'dbo.PhieuNhap')      IS NOT NULL DROP TABLE dbo.PhieuNhap;
IF OBJECT_ID(N'dbo.DongHo')         IS NOT NULL DROP TABLE dbo.DongHo;
IF OBJECT_ID(N'dbo.HangSanXuat')    IS NOT NULL DROP TABLE dbo.HangSanXuat;
IF OBJECT_ID(N'dbo.NhanVien')       IS NOT NULL DROP TABLE dbo.NhanVien;
IF OBJECT_ID(N'dbo.KhachHang')      IS NOT NULL DROP TABLE dbo.KhachHang;
IF OBJECT_ID(N'dbo.TaiKhoan')       IS NOT NULL DROP TABLE dbo.TaiKhoan;
GO

/* =====================================================================
   PHẦN CỦA PHƯƠNG — Tài khoản, hồ sơ, danh mục, kho
   ===================================================================== */

-- 1. Tài khoản đăng nhập (3 vai trò)
CREATE TABLE dbo.TaiKhoan (
    MaTK        VARCHAR(10)   NOT NULL CONSTRAINT PK_TaiKhoan PRIMARY KEY,   -- TK001
    Email       VARCHAR(100)  NOT NULL CONSTRAINT UQ_TaiKhoan_Email UNIQUE,
    MatKhauHash CHAR(64)      NOT NULL,                                      -- SHA256 hex
    Salt        VARCHAR(32)   NOT NULL,
    VaiTro      VARCHAR(20)   NOT NULL,
    TrangThai   VARCHAR(20)   NOT NULL CONSTRAINT DF_TaiKhoan_TrangThai DEFAULT 'HoatDong',
    NgayTao     DATETIME2(0)  NOT NULL CONSTRAINT DF_TaiKhoan_NgayTao DEFAULT SYSDATETIME(),
    CONSTRAINT CK_TaiKhoan_VaiTro    CHECK (VaiTro IN ('KhachHang','NhanVien','Admin')),
    CONSTRAINT CK_TaiKhoan_TrangThai CHECK (TrangThai IN ('HoatDong','Khoa'))
);

-- 2. Khách hàng (khách mua tại quầy có thể chưa có tài khoản -> MaTK NULL)
CREATE TABLE dbo.KhachHang (
    MaKH        VARCHAR(10)   NOT NULL CONSTRAINT PK_KhachHang PRIMARY KEY, -- KH001
    MaTK        VARCHAR(10)   NULL CONSTRAINT FK_KhachHang_TaiKhoan REFERENCES dbo.TaiKhoan(MaTK),
    HoTen       NVARCHAR(100) NOT NULL,
    SoDienThoai VARCHAR(15)   NULL,
    Email       VARCHAR(100)  NULL,
    DiaChi      NVARCHAR(250) NULL,
    NgayTao     DATETIME2(0)  NOT NULL CONSTRAINT DF_KhachHang_NgayTao DEFAULT SYSDATETIME()
);
-- Một tài khoản chỉ gắn với một hồ sơ khách
CREATE UNIQUE INDEX UX_KhachHang_MaTK ON dbo.KhachHang(MaTK) WHERE MaTK IS NOT NULL;

-- 3. Nhân viên (cả Admin cũng có hồ sơ nhân viên để ghi người thực hiện)
CREATE TABLE dbo.NhanVien (
    MaNV        VARCHAR(10)   NOT NULL CONSTRAINT PK_NhanVien PRIMARY KEY,  -- NV001
    MaTK        VARCHAR(10)   NOT NULL CONSTRAINT UQ_NhanVien_MaTK UNIQUE
                              CONSTRAINT FK_NhanVien_TaiKhoan REFERENCES dbo.TaiKhoan(MaTK),
    HoTen       NVARCHAR(100) NOT NULL,
    SoDienThoai VARCHAR(15)   NULL,
    ChucVu      NVARCHAR(50)  NOT NULL,
    NgayVaoLam  DATE          NOT NULL
);

-- 4. Hãng sản xuất
CREATE TABLE dbo.HangSanXuat (
    MaHang      VARCHAR(10)   NOT NULL CONSTRAINT PK_HangSanXuat PRIMARY KEY, -- HG001
    TenHang     NVARCHAR(100) NOT NULL CONSTRAINT UQ_HangSanXuat_Ten UNIQUE,
    QuocGia     NVARCHAR(50)  NULL,
    MoTa        NVARCHAR(500) NULL
);

-- 5. Mẫu đồng hồ (tồn kho KHÔNG lưu ở đây, đếm từ ChiTietDongHo)
CREATE TABLE dbo.DongHo (
    MaMau           VARCHAR(10)    NOT NULL CONSTRAINT PK_DongHo PRIMARY KEY, -- DH001
    TenMau          NVARCHAR(150)  NOT NULL,
    MaHang          VARCHAR(10)    NOT NULL CONSTRAINT FK_DongHo_Hang REFERENCES dbo.HangSanXuat(MaHang),
    Gia             DECIMAL(18,0)  NOT NULL,
    LoaiMay         NVARCHAR(50)   NOT NULL,      -- Automatic, Quartz, Lên cót tay
    KichThuoc       DECIMAL(4,1)   NOT NULL,      -- mm
    ChatLieu        NVARCHAR(100)  NOT NULL,
    MoTa            NVARCHAR(1000) NULL,
    HinhAnh         VARCHAR(255)   NULL,          -- /images/dh001.jpg
    ThoiHanBaoHanh  INT            NOT NULL CONSTRAINT DF_DongHo_BH DEFAULT 24,  -- tháng
    TrangThai       VARCHAR(20)    NOT NULL CONSTRAINT DF_DongHo_TrangThai DEFAULT 'DangBan',
    CONSTRAINT CK_DongHo_Gia       CHECK (Gia > 0),
    CONSTRAINT CK_DongHo_KichThuoc CHECK (KichThuoc > 0),
    CONSTRAINT CK_DongHo_BH        CHECK (ThoiHanBaoHanh BETWEEN 0 AND 120),
    CONSTRAINT CK_DongHo_TrangThai CHECK (TrangThai IN ('DangBan','NgungBan'))
);

-- 6. Phiếu nhập kho
CREATE TABLE dbo.PhieuNhap (
    MaPN        VARCHAR(10)   NOT NULL CONSTRAINT PK_PhieuNhap PRIMARY KEY, -- PN001
    NgayNhap    DATETIME2(0)  NOT NULL CONSTRAINT DF_PhieuNhap_Ngay DEFAULT SYSDATETIME(),
    MaNV        VARCHAR(10)   NOT NULL CONSTRAINT FK_PhieuNhap_NhanVien REFERENCES dbo.NhanVien(MaNV),
    GhiChu      NVARCHAR(250) NULL
);

-- 7. Từng chiếc đồng hồ theo serial
CREATE TABLE dbo.ChiTietDongHo (
    Serial      VARCHAR(30)   NOT NULL CONSTRAINT PK_ChiTietDongHo PRIMARY KEY,
    MaMau       VARCHAR(10)   NOT NULL CONSTRAINT FK_CTDH_DongHo REFERENCES dbo.DongHo(MaMau),
    MaPN        VARCHAR(10)   NOT NULL CONSTRAINT FK_CTDH_PhieuNhap REFERENCES dbo.PhieuNhap(MaPN),
    TinhTrang   VARCHAR(20)   NOT NULL CONSTRAINT DF_CTDH_TinhTrang DEFAULT 'Moi',
    CoHop       BIT           NOT NULL CONSTRAINT DF_CTDH_CoHop DEFAULT 1,
    CoGiayTo    BIT           NOT NULL CONSTRAINT DF_CTDH_CoGiayTo DEFAULT 1,
    TrangThai   VARCHAR(20)   NOT NULL CONSTRAINT DF_CTDH_TrangThai DEFAULT 'ConHang',
    CONSTRAINT CK_CTDH_TinhTrang CHECK (TinhTrang IN ('Moi','DaSuDung')),
    CONSTRAINT CK_CTDH_TrangThai CHECK (TrangThai IN ('ConHang','DaGiu','DaBan','KhongBan'))
);
CREATE INDEX IX_CTDH_MaMau_TrangThai ON dbo.ChiTietDongHo(MaMau, TrangThai);
GO

/* =====================================================================
   PHẦN CỦA HIỆP — Lịch hẹn showroom
   ===================================================================== */

-- 8. Lịch hẹn tư vấn / xem đồng hồ
CREATE TABLE dbo.LichHen (
    MaLH        VARCHAR(12)   NOT NULL CONSTRAINT PK_LichHen PRIMARY KEY,   -- LH20260001
    MaKH        VARCHAR(10)   NULL CONSTRAINT FK_LichHen_KhachHang REFERENCES dbo.KhachHang(MaKH),
    MaMau       VARCHAR(10)   NULL CONSTRAINT FK_LichHen_DongHo REFERENCES dbo.DongHo(MaMau),
    Showroom    NVARCHAR(100) NOT NULL,
    NgayHen     DATETIME2(0)  NOT NULL,
    HoTen       NVARCHAR(100) NOT NULL,
    SoDienThoai VARCHAR(15)   NOT NULL,
    GhiChu      NVARCHAR(250) NULL,
    TrangThai   VARCHAR(20)   NOT NULL CONSTRAINT DF_LichHen_TrangThai DEFAULT 'ChoXacNhan',
    MaNVXuLy    VARCHAR(10)   NULL CONSTRAINT FK_LichHen_NhanVien REFERENCES dbo.NhanVien(MaNV),
    LyDoHuy     NVARCHAR(250) NULL,
    NgayTao     DATETIME2(0)  NOT NULL CONSTRAINT DF_LichHen_NgayTao DEFAULT SYSDATETIME(),
    CONSTRAINT CK_LichHen_TrangThai CHECK (TrangThai IN ('ChoXacNhan','DaXacNhan','HoanTat','DaHuy')),
    CONSTRAINT CK_LichHen_Huy       CHECK (TrangThai <> 'DaHuy' OR LyDoHuy IS NOT NULL)
);
CREATE INDEX IX_LichHen_NgayHen ON dbo.LichHen(NgayHen);
GO

/* =====================================================================
   PHẦN CỦA NGUYÊN — Đơn hàng, hóa đơn, bảo hành
   ===================================================================== */

-- 9. Đơn hàng (online và tại quầy)
CREATE TABLE dbo.DonHang (
    MaDon         VARCHAR(12)   NOT NULL CONSTRAINT PK_DonHang PRIMARY KEY,  -- AM20260018
    MaKH          VARCHAR(10)   NOT NULL CONSTRAINT FK_DonHang_KhachHang REFERENCES dbo.KhachHang(MaKH),
    NguonDon      VARCHAR(10)   NOT NULL,
    MaNVLap       VARCHAR(10)   NULL CONSTRAINT FK_DonHang_NVLap  REFERENCES dbo.NhanVien(MaNV),
    MaNVXuLy      VARCHAR(10)   NULL CONSTRAINT FK_DonHang_NVXuLy REFERENCES dbo.NhanVien(MaNV),
    NgayDat       DATETIME2(0)  NOT NULL CONSTRAINT DF_DonHang_NgayDat DEFAULT SYSDATETIME(),
    TenNguoiNhan  NVARCHAR(100) NOT NULL,
    SoDienThoai   VARCHAR(15)   NOT NULL,
    HinhThucNhan  VARCHAR(20)   NOT NULL,
    DiaChiNhan    NVARCHAR(250) NULL,
    TongTien      DECIMAL(18,0) NOT NULL,
    TrangThai     VARCHAR(20)   NOT NULL CONSTRAINT DF_DonHang_TrangThai DEFAULT 'ChoXacNhan',
    PhuongThucTT  VARCHAR(20)   NULL,
    NgayHoanTat   DATETIME2(0)  NULL,
    LyDoHuy       NVARCHAR(250) NULL,
    CONSTRAINT CK_DonHang_Nguon     CHECK (NguonDon IN ('Online','TaiQuay')),
    CONSTRAINT CK_DonHang_NVLap     CHECK (NguonDon = 'Online' OR MaNVLap IS NOT NULL),
    CONSTRAINT CK_DonHang_HinhThuc  CHECK (HinhThucNhan IN ('GiaoTanNoi','NhanTaiShowroom')),
    CONSTRAINT CK_DonHang_DiaChi    CHECK (HinhThucNhan <> 'GiaoTanNoi' OR DiaChiNhan IS NOT NULL),
    CONSTRAINT CK_DonHang_TongTien  CHECK (TongTien >= 0),
    CONSTRAINT CK_DonHang_TrangThai CHECK (TrangThai IN ('ChoXacNhan','DaXacNhan','DangGiao','HoanTat','DaHuy')),
    CONSTRAINT CK_DonHang_PTTT      CHECK (PhuongThucTT IS NULL OR PhuongThucTT IN ('TienMat','ChuyenKhoan','The')),
    CONSTRAINT CK_DonHang_HoanTat   CHECK (TrangThai <> 'HoanTat' OR (NgayHoanTat IS NOT NULL AND PhuongThucTT IS NOT NULL)),
    CONSTRAINT CK_DonHang_Huy       CHECK (TrangThai <> 'DaHuy' OR LyDoHuy IS NOT NULL)
);
CREATE INDEX IX_DonHang_MaKH      ON dbo.DonHang(MaKH);
CREATE INDEX IX_DonHang_TrangThai ON dbo.DonHang(TrangThai);

-- 10. Chi tiết đơn: MỖI DÒNG = MỘT CHIẾC. Serial NULL khi chưa xác nhận.
CREATE TABLE dbo.ChiTietDonHang (
    MaDon   VARCHAR(12)   NOT NULL CONSTRAINT FK_CTDon_DonHang REFERENCES dbo.DonHang(MaDon),
    STT     INT           NOT NULL,
    MaMau   VARCHAR(10)   NOT NULL CONSTRAINT FK_CTDon_DongHo REFERENCES dbo.DongHo(MaMau),
    Serial  VARCHAR(30)   NULL CONSTRAINT FK_CTDon_Serial REFERENCES dbo.ChiTietDongHo(Serial),
    DonGia  DECIMAL(18,0) NOT NULL,                     -- giá chốt lúc đặt
    CONSTRAINT PK_ChiTietDonHang PRIMARY KEY (MaDon, STT),
    CONSTRAINT CK_CTDon_DonGia CHECK (DonGia >= 0)
);

-- 11. Hóa đơn (lập khi đơn hoàn tất, một đơn một hóa đơn)
CREATE TABLE dbo.HoaDon (
    MaHD         VARCHAR(12)   NOT NULL CONSTRAINT PK_HoaDon PRIMARY KEY,   -- HD20260001
    MaDon        VARCHAR(12)   NOT NULL CONSTRAINT UQ_HoaDon_MaDon UNIQUE
                               CONSTRAINT FK_HoaDon_DonHang REFERENCES dbo.DonHang(MaDon),
    NgayLap      DATETIME2(0)  NOT NULL CONSTRAINT DF_HoaDon_NgayLap DEFAULT SYSDATETIME(),
    MaNV         VARCHAR(10)   NOT NULL CONSTRAINT FK_HoaDon_NhanVien REFERENCES dbo.NhanVien(MaNV),
    TongTien     DECIMAL(18,0) NOT NULL,
    PhuongThucTT VARCHAR(20)   NOT NULL,
    CONSTRAINT CK_HoaDon_PTTT CHECK (PhuongThucTT IN ('TienMat','ChuyenKhoan','The'))
);

-- 12. Phiếu bảo hành
CREATE TABLE dbo.PhieuBaoHanh (
    MaPBH       VARCHAR(12)   NOT NULL CONSTRAINT PK_PhieuBaoHanh PRIMARY KEY, -- BH20260012
    MaDon       VARCHAR(12)   NOT NULL CONSTRAINT FK_PBH_DonHang REFERENCES dbo.DonHang(MaDon),
    Serial      VARCHAR(30)   NOT NULL CONSTRAINT FK_PBH_Serial REFERENCES dbo.ChiTietDongHo(Serial),
    MaKH        VARCHAR(10)   NOT NULL CONSTRAINT FK_PBH_KhachHang REFERENCES dbo.KhachHang(MaKH),
    NgayBatDau  DATE          NOT NULL,
    NgayHetHan  DATE          NOT NULL,
    MaNVLap     VARCHAR(10)   NOT NULL CONSTRAINT FK_PBH_NhanVien REFERENCES dbo.NhanVien(MaNV),
    GhiChu      NVARCHAR(250) NULL,
    CONSTRAINT UQ_PBH_Don_Serial UNIQUE (MaDon, Serial),          -- không lập trùng phiếu
    CONSTRAINT CK_PBH_Ngay CHECK (NgayHetHan >= NgayBatDau)
);
GO

/* =====================================================================
   DỮ LIỆU MẪU
   Tài khoản demo (mật khẩu 123456):
     admin@aurel.vn     Admin
     nhanvien@aurel.vn  NhanVien
     khach1@gmail.com   KhachHang (KH001 — đã mua DEMO012, có phiếu BH20260012)
     khach2@gmail.com   KhachHang (KH002 — có đơn AM20260018 đang chờ xác nhận)
   Giá chỉ để minh họa.
   ===================================================================== */

INSERT INTO dbo.TaiKhoan (MaTK, Email, MatKhauHash, Salt, VaiTro, TrangThai, NgayTao) VALUES
('TK001','admin@aurel.vn',    '15938cff25467c6bfa355af2a973acde230038eded64f6daeaef729e2630981c','a1f3c9e2b7d4','Admin',    'HoatDong','2026-10-01T08:00:00'),
('TK002','nhanvien@aurel.vn', 'f2598aa8e65621061d2f3cab7eadf9446432a6ad442d1a1672762627fc82065d','b8e2d1f4c6a9','NhanVien', 'HoatDong','2026-10-01T08:05:00'),
('TK003','khach1@gmail.com',  '475c9af9af821b236acb7a8bd1a71c4f08268fefd38fa338b50274247e729307','c3d7e9a1f2b5','KhachHang','HoatDong','2026-10-25T19:20:00'),
('TK004','khach2@gmail.com',  '2ce09368c99a149e6196a1f9c5cf1e5ddf398a57e28a556e63c6438790cab3cb','d9a4b2c8e1f7','KhachHang','HoatDong','2026-11-05T21:10:00');

INSERT INTO dbo.NhanVien (MaNV, MaTK, HoTen, SoDienThoai, ChucVu, NgayVaoLam) VALUES
('NV001','TK001',N'Quản trị viên',  '0905000001',N'Quản lý cửa hàng','2026-01-01'),
('NV002','TK002',N'Trần Thu Hà',    '0905000002',N'Tư vấn bán hàng', '2026-03-01');

INSERT INTO dbo.KhachHang (MaKH, MaTK, HoTen, SoDienThoai, Email, DiaChi, NgayTao) VALUES
('KH001','TK003',N'Nguyễn Văn An',   '0900111222','khach1@gmail.com',N'12 Lê Duẩn, Hải Châu, Đà Nẵng','2026-10-25T19:20:00'),
('KH002','TK004',N'Lê Thị Bình',     '0900123456','khach2@gmail.com',N'45 Nguyễn Văn Linh, Thanh Khê, Đà Nẵng','2026-11-05T21:10:00'),
('KH003', NULL,  N'Trần Quốc Cường', '0911222333', NULL,             NULL,'2026-11-02T15:00:00');   -- khách tại quầy, chưa có tài khoản

INSERT INTO dbo.HangSanXuat (MaHang, TenHang, QuocGia, MoTa) VALUES
('HG001',N'Rolex',            N'Thụy Sĩ',N'Đồng hồ cơ cao cấp, nổi bật với dòng Oyster Perpetual.'),
('HG002',N'Patek Philippe',   N'Thụy Sĩ',N'Thương hiệu đồng hồ cơ truyền thống, dòng Calatrava.'),
('HG003',N'Audemars Piguet',  N'Thụy Sĩ',N'Nổi tiếng với dòng Royal Oak vỏ bát giác.'),
('HG004',N'Cartier',          N'Pháp',   N'Đồng hồ thời trang cao cấp, dòng Santos và Tank.'),
('HG005',N'Omega',            N'Thụy Sĩ',N'Dòng Speedmaster và Seamaster.');

INSERT INTO dbo.DongHo (MaMau, TenMau, MaHang, Gia, LoaiMay, KichThuoc, ChatLieu, MoTa, HinhAnh, ThoiHanBaoHanh, TrangThai) VALUES
('DH001',N'Rolex Datejust 36',              'HG001',285000000,N'Automatic',  36.0,N'Thép Oystersteel',  N'Mặt số xám, vành khía, dây Jubilee.',      '/images/dh001.jpg',24,'DangBan'),
('DH002',N'Cartier Santos',                 'HG004',198000000,N'Automatic',  39.8,N'Thép không gỉ',     N'Vỏ vuông, mặt số trắng, dây da nâu.',      '/images/dh002.jpg',24,'DangBan'),
('DH003',N'Patek Philippe Calatrava',       'HG002',690000000,N'Lên cót tay',39.0,N'Vàng hồng 18K',     N'Thiết kế tối giản, dây da đen.',           '/images/dh003.jpg',24,'DangBan'),
('DH004',N'Audemars Piguet Royal Oak',      'HG003',950000000,N'Automatic',  41.0,N'Thép không gỉ',     N'Vỏ bát giác, mặt số Grande Tapisserie.',   '/images/dh004.jpg',24,'DangBan'),
('DH005',N'Omega Speedmaster Moonwatch',    'HG005',175000000,N'Lên cót tay',42.0,N'Thép không gỉ',     N'Bấm giờ thể thao, kính Hesalite.',         '/images/dh005.jpg',24,'DangBan');

INSERT INTO dbo.PhieuNhap (MaPN, NgayNhap, MaNV, GhiChu) VALUES
('PN001','2026-10-20T09:00:00','NV001',N'Nhập lô đầu tiên'),
('PN002','2026-11-03T10:30:00','NV001',N'Bổ sung Rolex Datejust');

-- Trước khi demo "Admin nhập DEMO001, DEMO002" có thể xóa 2 dòng này rồi nhập trực tiếp trên web.
INSERT INTO dbo.ChiTietDongHo (Serial, MaMau, MaPN, TinhTrang, CoHop, CoGiayTo, TrangThai) VALUES
('DEMO012','DH001','PN001','Moi',     1,1,'DaBan'),
('DEMO003','DH002','PN001','Moi',     1,1,'ConHang'),
('DEMO004','DH003','PN001','Moi',     1,1,'ConHang'),
('DEMO005','DH004','PN001','DaSuDung',1,0,'ConHang'),
('DEMO006','DH005','PN001','Moi',     1,1,'ConHang'),
('DEMO007','DH005','PN001','Moi',     0,1,'KhongBan'),
('DEMO001','DH001','PN002','Moi',     1,1,'ConHang'),
('DEMO002','DH001','PN002','Moi',     1,1,'ConHang');

INSERT INTO dbo.LichHen (MaLH, MaKH, MaMau, Showroom, NgayHen, HoTen, SoDienThoai, GhiChu, TrangThai, MaNVXuLy, LyDoHuy, NgayTao) VALUES
('LH20260001','KH002','DH003',N'Showroom Đà Nẵng','2026-11-15T10:00:00',N'Lê Thị Bình',  '0900123456',N'Muốn xem trực tiếp mặt số',      'ChoXacNhan',NULL,   NULL,'2026-11-06T20:00:00'),
('LH20260002','KH001','DH001',N'Showroom Đà Nẵng','2026-10-28T15:30:00',N'Nguyễn Văn An','0900111222',NULL,                              'HoanTat',  'NV002',NULL,'2026-10-26T09:00:00');

-- Đơn đã hoàn tất (nguồn dữ liệu cho hóa đơn, bảo hành, báo cáo)
INSERT INTO dbo.DonHang (MaDon, MaKH, NguonDon, MaNVLap, MaNVXuLy, NgayDat, TenNguoiNhan, SoDienThoai, HinhThucNhan, DiaChiNhan, TongTien, TrangThai, PhuongThucTT, NgayHoanTat, LyDoHuy) VALUES
('AM20260012','KH001','Online', NULL,   'NV002','2026-10-30T20:15:00',N'Nguyễn Văn An',  '0900111222','NhanTaiShowroom',NULL,285000000,'HoanTat',   'ChuyenKhoan','2026-11-01T10:00:00',NULL),
('AM20260015','KH003','TaiQuay','NV002','NV002','2026-11-02T15:05:00',N'Trần Quốc Cường','0911222333','NhanTaiShowroom',NULL,175000000,'DaHuy',     NULL,         NULL,                 N'Khách đổi ý trước khi thanh toán'),
('AM20260018','KH002','Online', NULL,   NULL,   '2026-11-08T21:30:00',N'Lê Thị Bình',    '0900123456','NhanTaiShowroom',NULL,285000000,'ChoXacNhan',NULL,         NULL,                 NULL);

INSERT INTO dbo.ChiTietDonHang (MaDon, STT, MaMau, Serial, DonGia) VALUES
('AM20260012',1,'DH001','DEMO012',285000000),
('AM20260015',1,'DH005',NULL,     175000000),
('AM20260018',1,'DH001',NULL,     285000000);   -- chờ nhân viên gán serial

INSERT INTO dbo.HoaDon (MaHD, MaDon, NgayLap, MaNV, TongTien, PhuongThucTT) VALUES
('HD20260001','AM20260012','2026-11-01T10:00:00','NV002',285000000,'ChuyenKhoan');

INSERT INTO dbo.PhieuBaoHanh (MaPBH, MaDon, Serial, MaKH, NgayBatDau, NgayHetHan, MaNVLap, GhiChu) VALUES
('BH20260012','AM20260012','DEMO012','KH001','2026-11-01','2028-11-01','NV002',N'Đủ hộp và giấy tờ');
GO

/* =====================================================================
   VIEW BÁO CÁO (Phương dùng cho Tổng quan + Báo cáo kinh doanh)
   ===================================================================== */

-- Tồn kho theo mẫu: chỉ đếm từ danh sách serial
CREATE VIEW dbo.vw_TonKhoTheoMau AS
SELECT d.MaMau, d.TenMau, h.TenHang, d.Gia, d.TrangThai,
       SUM(CASE WHEN c.TrangThai = 'ConHang'  THEN 1 ELSE 0 END) AS SoConHang,
       SUM(CASE WHEN c.TrangThai = 'DaGiu'    THEN 1 ELSE 0 END) AS SoDaGiu,
       SUM(CASE WHEN c.TrangThai = 'DaBan'    THEN 1 ELSE 0 END) AS SoDaBan,
       SUM(CASE WHEN c.TrangThai = 'KhongBan' THEN 1 ELSE 0 END) AS SoKhongBan
FROM dbo.DongHo d
JOIN dbo.HangSanXuat h   ON h.MaHang = d.MaHang
LEFT JOIN dbo.ChiTietDongHo c ON c.MaMau = d.MaMau
GROUP BY d.MaMau, d.TenMau, h.TenHang, d.Gia, d.TrangThai;
GO

-- Doanh thu: chỉ đơn HoanTat, giá đã chốt trên chi tiết đơn, theo ngày hoàn tất
CREATE VIEW dbo.vw_DoanhThuHoanTat AS
SELECT dh.MaDon, CAST(dh.NgayHoanTat AS DATE) AS NgayHoanTat, dh.NguonDon,
       dh.MaNVXuLy, nv.HoTen AS TenNhanVien,
       ct.MaMau, d.TenMau, ct.Serial, ct.DonGia
FROM dbo.DonHang dh
JOIN dbo.ChiTietDonHang ct ON ct.MaDon = dh.MaDon
JOIN dbo.DongHo d          ON d.MaMau  = ct.MaMau
LEFT JOIN dbo.NhanVien nv  ON nv.MaNV  = dh.MaNVXuLy
WHERE dh.TrangThai = 'HoanTat';
GO

/* Ví dụ báo cáo theo khoảng ngày:
DECLARE @Tu DATE = '2026-11-01', @Den DATE = '2026-11-30';
SELECT SUM(DonGia) AS DoanhThu, COUNT(*) AS SoChiecBan
FROM dbo.vw_DoanhThuHoanTat WHERE NgayHoanTat BETWEEN @Tu AND @Den;

SELECT TOP 5 MaMau, TenMau, COUNT(*) AS SoChiec, SUM(DonGia) AS DoanhThu
FROM dbo.vw_DoanhThuHoanTat WHERE NgayHoanTat BETWEEN @Tu AND @Den
GROUP BY MaMau, TenMau ORDER BY SoChiec DESC, DoanhThu DESC;

SELECT MaNVXuLy, TenNhanVien, COUNT(DISTINCT MaDon) AS SoDon, SUM(DonGia) AS DoanhSo
FROM dbo.vw_DoanhThuHoanTat WHERE NgayHoanTat BETWEEN @Tu AND @Den
GROUP BY MaNVXuLy, TenNhanVien;
*/

/* =====================================================================
   XUẤT SQL -> XML (đúng quy ước file của nhóm)
   Gốc <DanhSach<Bảng>>, mỗi dòng <Bảng>, mỗi cột một phần tử con.
   Cột NULL bị bỏ qua -> trong XSD khai báo minOccurs="0" cho cột cho phép NULL.
   DongBoService (Phương) có thể chạy các câu này rồi lưu kết quả ra
   App_Data/Xml/<Bảng>.xml, hoặc dùng DataTable.WriteXml như mẫu của thầy.
   ===================================================================== */

SELECT * FROM dbo.TaiKhoan       FOR XML PATH('TaiKhoan'),       ROOT('DanhSachTaiKhoan'),       ELEMENTS;
SELECT * FROM dbo.KhachHang      FOR XML PATH('KhachHang'),      ROOT('DanhSachKhachHang'),      ELEMENTS;
SELECT * FROM dbo.NhanVien       FOR XML PATH('NhanVien'),       ROOT('DanhSachNhanVien'),       ELEMENTS;
SELECT * FROM dbo.HangSanXuat    FOR XML PATH('HangSanXuat'),    ROOT('DanhSachHangSanXuat'),    ELEMENTS;
SELECT * FROM dbo.DongHo         FOR XML PATH('DongHo'),         ROOT('DanhSachDongHo'),         ELEMENTS;
SELECT * FROM dbo.PhieuNhap      FOR XML PATH('PhieuNhap'),      ROOT('DanhSachPhieuNhap'),      ELEMENTS;
SELECT Serial, MaMau, MaPN, TinhTrang,
       CASE CoHop    WHEN 1 THEN 'true' ELSE 'false' END AS CoHop,      -- bit -> true/false cho xs:boolean
       CASE CoGiayTo WHEN 1 THEN 'true' ELSE 'false' END AS CoGiayTo,
       TrangThai
FROM dbo.ChiTietDongHo           FOR XML PATH('ChiTietDongHo'),  ROOT('DanhSachChiTietDongHo'),  ELEMENTS;
SELECT * FROM dbo.LichHen        FOR XML PATH('LichHen'),        ROOT('DanhSachLichHen'),        ELEMENTS;
SELECT * FROM dbo.DonHang        FOR XML PATH('DonHang'),        ROOT('DanhSachDonHang'),        ELEMENTS;
SELECT * FROM dbo.ChiTietDonHang FOR XML PATH('ChiTietDonHang'), ROOT('DanhSachChiTietDonHang'), ELEMENTS;
SELECT * FROM dbo.HoaDon         FOR XML PATH('HoaDon'),         ROOT('DanhSachHoaDon'),         ELEMENTS;
SELECT * FROM dbo.PhieuBaoHanh   FOR XML PATH('PhieuBaoHanh'),   ROOT('DanhSachPhieuBaoHanh'),   ELEMENTS;
GO

/* Mẫu dữ liệu Web Service bảo hành trả về (Nguyên) — chỉ phiếu của khách đang đăng nhập:
DECLARE @MaPBH VARCHAR(12) = 'BH20260012', @MaKH VARCHAR(10) = 'KH001';
SELECT p.MaPBH AS MaPhieu, p.MaDon, d.TenMau, p.Serial, p.NgayBatDau, p.NgayHetHan,
       CASE WHEN p.NgayHetHan >= CAST(GETDATE() AS DATE) THEN 'ConBaoHanh' ELSE 'HetBaoHanh' END AS TrangThai
FROM dbo.PhieuBaoHanh p
JOIN dbo.ChiTietDongHo c ON c.Serial = p.Serial
JOIN dbo.DongHo d        ON d.MaMau  = c.MaMau
WHERE p.MaPBH = @MaPBH AND p.MaKH = @MaKH
FOR XML PATH('PhieuBaoHanh'), ELEMENTS;
*/
