using Microsoft.AspNetCore.Mvc;
namespace QuanLyChiTieuCaNhan.Controllers;
public class HomeController : Controller
{
    public IActionResult Index() => RedirectToAction("Index", "Dashboard");
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() { Response.StatusCode = 500; return View("~/Views/Shared/Error.cshtml"); }
}
