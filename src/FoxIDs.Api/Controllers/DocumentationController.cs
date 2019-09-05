using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FoxIDs.Controllers
{
    public class DocumentationController : Controller
    {
        private readonly IApiDescriptionGroupCollectionProvider _apiExplorer;
        public DocumentationController(IApiDescriptionGroupCollectionProvider apiExplorer)
        {
            _apiExplorer = apiExplorer;
        }

        public IActionResult Index()
        {
            return View(_apiExplorer);
        }
    }
}
