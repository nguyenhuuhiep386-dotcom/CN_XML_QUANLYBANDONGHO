using AurelMaison.Models;
using AurelMaison.Services;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace AurelMaison.Controllers
{
    public class HomeController(IDanhMucService catalog) : Controller
    {
        public IActionResult Index()
        {
            return View(new TrangChuVm(catalog.LayHang(),
                catalog.LayDanhSach(new(null, null, null, null, null, null)).Take(3).ToList()));
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
