using AurelMaison.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AurelMaison.Areas.Admin.Controllers;

/// <summary>xmldata · Dữ liệu XML (kiểm tra, đồng bộ SQL ↔ XML, sao lưu). Làm ở GĐ 5 cùng DongBoService.</summary>
[Area("Admin")]
[Authorize(Roles = VaiTro.Admin)]
public class DuLieuController : Controller
{
    public IActionResult Index() => View("ChuaTrienKhai");
}
