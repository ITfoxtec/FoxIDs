using FoxIDs.Models;
using FoxIDs.Models.Sequences;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoxIDs.Logic
{
    public class LogicBase
    {
        private readonly IHttpContextAccessor httpContextAccessor;

        public LogicBase([FromServices] IHttpContextAccessor httpContextAccessor)
        {
            this.httpContextAccessor = httpContextAccessor;
        }

        public HttpContext HttpContext => httpContextAccessor.HttpContext;

        public RouteBinding RouteBinding => HttpContext.GetRouteBinding();

        public Sequence Sequence => HttpContext.GetSequence();

        public string SequenceString => HttpContext.GetSequenceString();
    }
}
