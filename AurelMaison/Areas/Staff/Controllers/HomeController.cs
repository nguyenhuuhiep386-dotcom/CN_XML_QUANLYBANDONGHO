using AurelMaison.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AurelMaison.Areas.Staff.Controllers;

[Area("Staff")]
[Authorize(Roles = VaiTro.NhanVienHoacAdmin)]
public class HomeController : Controller
{
    public IActionResult Index() => View("ChuaTrienKhai");
}
