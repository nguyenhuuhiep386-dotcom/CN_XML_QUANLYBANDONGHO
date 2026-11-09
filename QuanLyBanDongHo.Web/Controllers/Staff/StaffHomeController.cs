using AurelMaison.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AurelMaison.Controllers.Staff;

[Authorize(Roles = VaiTro.NhanVienHoacAdmin)]
[Route("Staff")]
public class StaffHomeController : Controller
{
    [HttpGet("")]
    public IActionResult Index() => View("ChuaTrienKhai");
}
