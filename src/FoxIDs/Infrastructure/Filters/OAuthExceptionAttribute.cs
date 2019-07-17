using ITfoxtec.Identity;
using ITfoxtec.Identity.Messages;
using FoxIDs.Logic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;

namespace FoxIDs.Infrastructure.Filters
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class OAuthExceptionAttribute : TypeFilterAttribute
    {
        public OAuthExceptionAttribute() : base(typeof(OAuthExceptionActionAttribute))
        {
        }

        public class OAuthExceptionActionAttribute : ExceptionFilterAttribute
        {
            private readonly TelemetryScopedLogger logger;

            public OAuthExceptionActionAttribute(TelemetryScopedLogger logger)
            {
                this.logger = logger;
            }

            public override void OnException(ExceptionContext context)
            {
                if (context.Exception != null)
                {
                    logger.Error(context.Exception);

                    var tokenResponse = new TokenResponse();

                    if (context.Exception is OAuthRequestException)
                    {
                        context.Result = OAuthRequestExceptionToJsonResult(context.Exception as OAuthRequestException);
                    }
                    else if (context.Exception.InnerException is OAuthRequestException)
                    {
                        context.Result = OAuthRequestExceptionToJsonResult(context.Exception.InnerException as OAuthRequestException);
                    }
                    else
                    {
                        context.Result = new JsonResult(new TokenResponse { Error = IdentityConstants.ResponseErrors.InvalidRequest });
                    }
                }
            }

            private IActionResult OAuthRequestExceptionToJsonResult(OAuthRequestException oAuthRequestException)
            {
                return new JsonResult(new TokenResponse
                {
                    Error = oAuthRequestException.Error,
                    ErrorDescription = oAuthRequestException.ErrorDescription,
                });
            }
        }
    }
}
