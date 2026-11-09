using Microsoft.AspNetCore.Mvc;

namespace AurelMaison.Controllers;

public class AccountController : Controller
{
    public IActionResult Login() => View("ChuaTrienKhai");

    public IActionResult AccessDenied() => View("ChuaTrienKhai");
}
