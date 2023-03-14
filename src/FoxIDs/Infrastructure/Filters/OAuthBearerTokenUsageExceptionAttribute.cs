using ITfoxtec.Identity;
using ITfoxtec.Identity.Messages;
using FoxIDs.Logic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using Microsoft.Net.Http.Headers;
using Microsoft.Extensions.Primitives;
using System.Collections.Generic;
using System.Web;

namespace FoxIDs.Infrastructure.Filters
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class OAuthBearerTokenUsageExceptionAttribute : TypeFilterAttribute
    {
        public OAuthBearerTokenUsageExceptionAttribute() : base(typeof(OAuthBearerTokenUsageExceptionActionAttribute))
        {
        }

        public class OAuthBearerTokenUsageExceptionActionAttribute : ExceptionFilterAttribute
        {
            private readonly TelemetryScopedLogger logger;

            public OAuthBearerTokenUsageExceptionActionAttribute(TelemetryScopedLogger logger)
            {
                this.logger = logger;
            }

            public override void OnException(ExceptionContext context)
            {
                if (context.Exception != null)
                {
                    logger.Error(context.Exception);

                    if (context.Exception is OAuthRequestException)
                    {
                        OAuthRequestExceptionToBearerTokenUsageError(context, context.Exception as OAuthRequestException);
                    }
                    else if (context.Exception.InnerException is OAuthRequestException)
                    {
                        OAuthRequestExceptionToBearerTokenUsageError(context, context.Exception.InnerException as OAuthRequestException);
                    }
                    else
                    {
                        context.Result = new JsonResult(new TokenResponse { Error = IdentityConstants.ResponseErrors.InvalidRequest });
                    }
                }
            }

            private void OAuthRequestExceptionToBearerTokenUsageError(ExceptionContext context, OAuthRequestException oAuthRequestException)
            {
                var headerValues = new List<string> { IdentityConstants.TokenTypes.Bearer, $"error {oAuthRequestException.Error}" };
                if (!oAuthRequestException.ErrorDescription.IsNullOrWhiteSpace())
                {
                    headerValues.Add($"error_description {oAuthRequestException.ErrorDescription}");
                }
                context.HttpContext.Response.Headers[HeaderNames.WWWAuthenticate] = new StringValues(headerValues.ToArray()); 
                context.Result = new UnauthorizedResult();
            }
        }
    }
}
