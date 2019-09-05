using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FoxIDs.Infrastructure.Swagger
{
    public class SwaggerRouteAttribute : RouteAttribute
    {
        public SwaggerRouteAttribute(string template) : base(ChangeTemplate(template))
        { }

        private static string ChangeTemplate(string template)
        {
            return "" + template.Replace("Controller", "");
        }
    }
}
