using AurelMaison.Models;
using AurelMaison.Services;
using System.Xml.Linq;
using AurelMaison.Controllers.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

var source = Path.GetFullPath(args.Length > 0 ? args[0] : "AurelMaison");
var temp = Path.Combine(AppContext.BaseDirectory, "AurelMaison-DonHang-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(temp);
try
{
    foreach (var dir in new[] { "Xml", "Xsd", "Xslt" })
    {
        var dest = Path.Combine(temp, "App_Data", dir);
        Directory.CreateDirectory(dest);
        foreach (var file in Directory.GetFiles(Path.Combine(source, "App_Data", dir)))
            File.Copy(file, Path.Combine(dest, Path.GetFileName(file)));
    }
    XmlHelper.KhoiTao(temp);
    void Check(bool ok, string label)
    {
        if (!ok) throw new Exception(label);
        Console.WriteLine("PASS: " + label);
    }
    foreach (var table in new[] { "DonHang", "ChiTietDonHang", "HoaDon", "PhieuBaoHanh" })
    {
        var doc = XmlHelper.Load(table);
        Check(XmlHelper.KiemTraXsd(table, doc).Count == 0, table + " sample validates");
        doc.Root!.Add(new XElement(doc.Root.Elements().First()));
        Check(XmlHelper.KiemTraXsd(table, doc).Count > 0, table + " duplicate key rejected");
    }
    var kho = new KhoService();
    var service = new DonHangService(new DanhMucService(kho), kho);
    var nhan = new ThongTinNhanHang("Khách thử", "0901234567", "NhanTaiShowroom", Showroom: "Showroom Đà Nẵng");
    var before = XmlHelper.Load("DonHang").ToString();
    Check(!service.TaoDonOnline("KH002", [new("DH001", 0)], nhan).ThanhCong, "reject zero quantity");
    Check(!service.TaoDonOnline("KH002", [new("DH001", int.MaxValue), new("DH001", int.MaxValue)], nhan).ThanhCong, "aggregate duplicate items without overflow");
    Check(!service.TaoDonOnline("KH999", [new("DH001", 1)], nhan).ThanhCong, "reject unknown customer");
    Check(!service.TaoDonOnline("KH002", [new("DH001", 1)], nhan with { Showroom = "" }).ThanhCong, "require showroom");
    Check(XmlHelper.Load("DonHang").ToString() == before, "failed requests do not write orders");
    var stock = kho.DemConHang("DH001");
    Check(stock >= 2, "fixture has two available watches");
    var result = service.TaoDonOnline("KH002", [new("DH001", 1), new("DH001", 1)], nhan);
    Check(result.ThanhCong && result.TongTien == 570000000m, "server calculates total: " + result.ThongBao);
    var detail = service.LayChiTietTheoKhach(result.Ma, "KH002")!;
    var lines = detail.Element("ChiTiet")!.Elements().ToList();
    Check(lines.Count == 2 && lines.All(x => x.Element("Serial") == null), "one line per watch without reserving serial");
    Check(kho.DemConHang("DH001") == stock, "online creation does not reserve stock");
    Check(service.LayChiTietTheoKhach(result.Ma, "KH001") == null, "other customer cannot read order");
    XmlHelper.Sua("DongHo", "MaMau", "DH001", x => { x.SetElementValue("Gia", 1); x.SetElementValue("ThoiHanBaoHanh", 12); });
    detail = service.LayChiTietTheoKhach(result.Ma, "KH002")!;
    Check(detail.Element("ChiTiet")!.Elements().All(x => (decimal)x.Element("DonGia")! == 285000000m && (int)x.Element("ThoiHanBaoHanh")! == 24), "sale snapshots survive catalog changes");
    var invalid = XmlHelper.Load("ChiTietDonHang");
    invalid.Root!.Elements().First().SetElementValue("DonGia", -1);
    Check(XmlHelper.KiemTraXsd("ChiTietDonHang", invalid).Count > 0, "reject negative price");
    var bh = new BaoHanhService(service);
    Check(!bh.LapPhieu(result.Ma, "DEMO001", "NV002").ThanhCong, "reject warranty before completion");
    Check(!service.XacNhan(result.Ma, ["DEMO001"], "NV002").ThanhCong, "reject missing serial");
    Check(!service.XacNhan(result.Ma, ["DEMO001", "DEMO001"], "NV002").ThanhCong, "reject duplicate serial");
    Check(!service.XacNhan(result.Ma, ["DEMO001", "DEMO003"], "NV002").ThanhCong, "reject wrong model without partial reservation");
    Check(kho.DemConHang("DH001") == stock, "failed confirmation leaves all stock available");
    Check(!service.XacNhan(result.Ma, ["DEMO001", "DEMO002"], "NV999").ThanhCong, "reject unknown employee");
    Check(service.XacNhan(result.Ma, ["DEMO001", "DEMO002"], "NV002").ThanhCong, "confirm and reserve all serials");
    Check(kho.DemConHang("DH001") == stock - 2, "reserved serials unavailable");
    Check(!service.XacNhan("AM20260018", ["DEMO001"], "NV001").ThanhCong, "another order cannot reuse reserved serial");
    Check(!service.Huy(result.Ma, "KH002", "Đổi ý").ThanhCong, "customer cannot cancel confirmed order");
    Check(!service.GiaoHang(result.Ma, "NV002").ThanhCong, "showroom order cannot enter delivery");
    Check(!service.HoanTat(result.Ma, "NV001", "Invalid").ThanhCong, "reject invalid payment");
    Check(service.HoanTat(result.Ma, "NV001", "The").ThanhCong, "admin completes employee order");
    var completed = service.LayChiTiet(result.Ma)!;
    Check((string?)completed.Element("MaNVPhuTrach") == "NV002" && (string?)completed.Element("HoaDon")!.Element("MaNV") == "NV001", "sales credit stays with confirmer");
    Check(kho.DemConHang("DH001") == stock - 2 && kho.LayDanhSachChiec(trangThai: "DaBan").Count(x => x.Serial is "DEMO001" or "DEMO002") == 2, "completion marks sold without double decrement");
    Check(!service.HoanTat(result.Ma, "NV001", "The").ThanhCong, "reject repeated completion");
    Check(!service.HuyBoiNhanVien(result.Ma, "NV002", "Hủy").ThanhCong, "reject cancel completed order");
    var html = service.XuatHoaDon((string)completed.Element("HoaDon")!.Element("MaHD")!);
    Check(html != null && html.Contains("285,000,000") && html.Contains("Rolex Datejust 36"), "invoice XSLT uses price and name snapshot");
    var warranty = bh.LapPhieu(result.Ma, "DEMO001", "NV002");
    Check(warranty.ThanhCong, "create warranty");
    var phieu = bh.LayPhieu(warranty.Ma)!;
    Check((DateTime)phieu.Element("NgayHetHan")! == ((DateTime)completed.Element("NgayHoanTat")!).Date.AddMonths(24), "warranty uses original 24 months");
    Check(!bh.LapPhieu(result.Ma, "DEMO001", "NV002").ThanhCong, "reject duplicate warranty");
    Check(!bh.LapPhieu(result.Ma, "DEMO003", "NV002").ThanhCong, "reject serial outside order");
    var xml = bh.TaoPhanHoi(phieu);
    Check(xml.Element("MaPhieu")?.Value == warranty.Ma && xml.Element("MaPBH") == null && xml.Elements().Count() == 7, "public warranty XML contract");
    BaoHanhApiController Api(string kh) => new(bh) { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(CurrentUser.ClaimMaKH, kh)], "test")) } } };
    Check(Api("KH002").TraCuu(warranty.Ma) is ContentResult { ContentType: "application/xml; charset=utf-8" }, "API owner gets XML");
    Check(Api("KH001").TraCuu(warranty.Ma) is StatusCodeResult { StatusCode: 403 }, "API other customer gets 403");
    Check(Api("KH002").TraCuu("BH99999999") is NotFoundResult, "API missing warranty gets 404");
    var list = (ContentResult)Api("KH002").DanhSach();
    Check(XElement.Parse(list.Content!).Elements().All(x => (string?)x.Element("MaDon") == result.Ma), "API list includes only current customer");

    Check(!service.Huy("AM20260018", "KH001", "Đổi ý").ThanhCong, "other customer cannot cancel pending order");
    Check(service.Huy("AM20260018", "KH002", "Đổi ý").ThanhCong, "owner cancels pending order");
    var delivery = service.TaoDonOnline("KH002", [new("DH002",1)], new("Khách", "0901234567", "GiaoTanNoi", "Đà Nẵng"));
    Check(delivery.ThanhCong && service.XacNhan(delivery.Ma, ["DEMO003"], "NV002").ThanhCong, "prepare delivery order");
    Check(!service.HuyBoiNhanVien(delivery.Ma,"NV001", " ").ThanhCong, "cancellation requires reason");
    Check(service.HuyBoiNhanVien(delivery.Ma,"NV001", "Khách đổi ý").ThanhCong && kho.LaySerialConHang("DH002").Contains("DEMO003"), "staff cancellation releases reservation");
    var delivery2 = service.TaoDonOnline("KH002", [new("DH002",1)], new("Khách", "0901234567", "GiaoTanNoi", "Đà Nẵng"));
    Check(service.XacNhan(delivery2.Ma, ["DEMO003"], "NV002").ThanhCong && service.GiaoHang(delivery2.Ma,"NV002").ThanhCong, "confirmed delivery moves to shipping");
    Check(!service.HuyBoiNhanVien(delivery2.Ma,"NV001", "Đổi ý").ThanhCong, "cannot cancel shipping order");
    Check(service.HoanTat(delivery2.Ma,"NV002","TienMat").ThanhCong, "complete delivery");
    var beforePos = XmlHelper.Load("DonHang").ToString();
    Check(!service.TaoDonTaiQuay("KH003", ["DEMO004","DEMO001"], "NV002", "TienMat").ThanhCong, "POS rejects a sold serial in batch");
    Check(XmlHelper.Load("DonHang").ToString()==beforePos && kho.LaySerialConHang("DH003").Contains("DEMO004"), "failed POS writes no partial sale");
    var pos=service.TaoDonTaiQuay("KH003", ["DEMO004"], "NV002", "ChuyenKhoan");
    Check(pos.ThanhCong && (string?)service.LayChiTiet(pos.Ma)!.Element("TrangThai")=="HoanTat", "POS completes immediately");
    Check(service.LayChiTiet(pos.Ma)!.Element("HoaDon")!=null && kho.LayDanhSachChiec(trangThai:"DaBan").Any(x=>x.Serial=="DEMO004"), "POS creates invoice and sells serial");
    var lich=new LichHenService(new CustomerGroupAdapter(new AuthService(), new KhachHangService(), new DanhMucService(kho), kho));
    Check(!lich.HoanTat("LH20260001","NV002").ThanhCong, "appointment requires confirmation");
    Check(lich.XacNhan("LH20260001","NV002").ThanhCong, "confirm appointment");
    Check(!lich.Huy("LH20260001","NV002", "").ThanhCong, "appointment cancellation requires reason");
    Check(lich.HoanTat("LH20260001","NV001").ThanhCong && !lich.Huy("LH20260001","NV002","Đổi ý").ThanhCong, "complete appointment and reject further changes");
    var race1 = service.TaoDonOnline("KH001", [new("DH005",1)], nhan);
    var race2 = service.TaoDonOnline("KH002", [new("DH005",1)], nhan);
    Check(race1.ThanhCong && race2.ThanhCong, "pending orders do not reserve the same available stock");
    var races = await Task.WhenAll(Task.Run(()=>service.XacNhan(race1.Ma,["DEMO006"],"NV002")), Task.Run(()=>service.XacNhan(race2.Ma,["DEMO006"],"NV001")));
    Check(races.Count(x=>x.ThanhCong)==1, "concurrent confirmations reserve a serial only once");
    var winner = races[0].ThanhCong ? race1.Ma : race2.Ma;
    Check(service.HuyBoiNhanVien(winner,"NV001","Kiểm tra đồng thời").ThanhCong && kho.LaySerialConHang("DH005").Contains("DEMO006"), "winning reservation releases correctly");
    foreach(var file in Directory.GetFiles(Path.Combine(temp,"App_Data","Xml"),"*.xml"))
    {
        var name=Path.GetFileNameWithoutExtension(file);
        Check(XmlHelper.KiemTraXsd(name,XmlHelper.Load(name)).Count==0,name+" final XSD valid");
    }
    Console.WriteLine("All checks passed; original XML untouched.");
}
finally
{
    Directory.Delete(temp, recursive: true);
}
