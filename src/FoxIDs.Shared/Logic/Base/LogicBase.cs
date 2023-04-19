using FoxIDs.Models;
using Microsoft.AspNetCore.Http;

namespace FoxIDs.Logic
{
    public class LogicBase : IRouteBinding
    {
        private readonly IHttpContextAccessor httpContextAccessor;

        public LogicBase(IHttpContextAccessor httpContextAccessor)
        {
            this.httpContextAccessor = httpContextAccessor;
        }

        public HttpContext HttpContext => httpContextAccessor.HttpContext;

        public RouteBinding RouteBinding => HttpContext.GetRouteBinding();
    }
}
