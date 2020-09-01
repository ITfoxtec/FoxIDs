//using FoxIDs.Models.ViewModels;
//using Microsoft.AspNetCore.Mvc;

//namespace FoxIDs.Controllers.Client
//{
//    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
//    //TODO [FoxIDsHttpSecurityHeaders]
//    public class ClientController : Controller
//    {
//        public IActionResult Index()
//        {
//            var routeBinding = HttpContext.GetRouteBinding();

//            return View(new ClientViewModel { TenantName = routeBinding.TenantName });
//        }
//    }
//}
