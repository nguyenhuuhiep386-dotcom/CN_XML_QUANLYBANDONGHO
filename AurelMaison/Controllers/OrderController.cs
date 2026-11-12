using AurelMaison.Models;
using AurelMaison.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AurelMaison.Controllers;

[Authorize(Roles = Security.Customer)]
public sealed class OrderController(GioHangService cart, IKhachHangService customers, IDonHangService orders) : Controller
{
    [HttpGet]
    public IActionResult Checkout()
    {
        var summary = cart.Xem(HttpContext.Session);
        if (!summary.HopLe) return InvalidCart();
        var customer = customers.LayTheoTaiKhoan(User.MaTK()!);
        if (customer is null) return NotFound();
        return CheckoutView(new DatHangForm { TenNguoiNhan = customer.HoTen, SoDienThoai = customer.SoDienThoai ?? "",
            DiaChiNhan = customer.DiaChi, Showroom = Rules.Showroom }, summary);
    }

    [HttpPost]
    public IActionResult Checkout(DatHangForm form)
    {
        var summary = cart.Xem(HttpContext.Session);
        if (!summary.HopLe) return InvalidCart();
        if (!ModelState.IsValid) return CheckoutView(form, summary);

        var items = cart.Lay(HttpContext.Session);
        var result = orders.TaoDonOnline(User.MaKH()!, items, form);
        if (!result.ThanhCong || string.IsNullOrWhiteSpace(result.Ma))
        {
            ModelState.AddModelError("", result.ThongBao);
            return CheckoutView(form, cart.Xem(HttpContext.Session));
        }
        cart.XoaCacMucDaDat(HttpContext.Session, items);
        return RedirectToAction(nameof(Success), new { id = result.Ma });
    }

    [HttpGet]
    public IActionResult Success(string id) => OwnOrder(id) is { } order ? View(order) : NotFound();

    [HttpGet]
    public IActionResult Index() => RedirectToAction(nameof(Tracking));

    [HttpGet]
    public IActionResult Tracking() => View(orders.LayTheoKhach(User.MaKH()!));

    [HttpGet]
    public IActionResult ChiTiet(string id) => OwnOrder(id) is { } order ? View(order) : NotFound();

    [HttpPost]
    public IActionResult Huy(string id, string? lyDo)
    {
        if (OwnOrder(id) is not { } order) return NotFound();
        var result = orders.Huy(order.MaDon, User.MaKH()!, lyDo ?? "");
        TempData[result.ThanhCong ? "Message" : "Error"] = result.ThongBao;
        return RedirectToAction(nameof(Cancel), new { id = order.MaDon });
    }

    [HttpGet]
    public IActionResult Cancel(string id) => OwnOrder(id) is { } order ? View(order) : NotFound();

    private DonHang? OwnOrder(string? id) => string.IsNullOrWhiteSpace(id) ? null : orders.LayChiTiet(id, User.MaKH()!);

    private IActionResult CheckoutView(DatHangForm form, GioVm summary)
    {
        ViewData["Gio"] = summary;
        return View("Checkout", form);
    }

    private IActionResult InvalidCart()
    {
        TempData["Error"] = "Giỏ trống hoặc số lượng vượt tồn hiện tại. Hãy kiểm tra lại giỏ.";
        return RedirectToAction("Index", "Cart");
    }
}
