using Microsoft.AspNetCore.Http;
using System;

namespace FoxIDs.SeedTool.Model
{
    public class HttpContextAccessorSeedHelper : IHttpContextAccessor
    {
        public HttpContext HttpContext { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    }
}
