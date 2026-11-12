using AurelMaison.Models;
using AurelMaison.Services;
using Microsoft.AspNetCore.Mvc;

namespace AurelMaison.Controllers;

public sealed class CartController(GioHangService cart) : Controller
{
    [HttpGet]
    public IActionResult Index() => View(cart.Xem(HttpContext.Session));

    [HttpPost]
    public IActionResult Them(string maMau, int soLuong)
    {
        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(maMau)) return BadRequest();
        return Show(cart.Them(HttpContext.Session, maMau, soLuong));
    }

    [HttpPost]
    public IActionResult CapNhat(string maMau, int soLuong)
    {
        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(maMau)) return BadRequest();
        return Show(cart.CapNhat(HttpContext.Session, maMau, soLuong));
    }

    [HttpPost]
    public IActionResult Xoa(string maMau)
    {
        if (string.IsNullOrWhiteSpace(maMau)) return BadRequest();
        cart.Xoa(HttpContext.Session, maMau);
        return Show(CustomerResult.Ok("Đã xóa sản phẩm khỏi giỏ hàng."));
    }

    private IActionResult Show(KetQua result)
    {
        TempData[result.ThanhCong ? "Message" : "Error"] = result.ThongBao;
        return RedirectToAction(nameof(Index));
    }
}
