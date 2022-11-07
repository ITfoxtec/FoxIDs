using FoxIDs.Logic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace FoxIDs.Infrastructure.Filters
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class LogAttribute : TypeFilterAttribute
    {
        public LogAttribute() : base(typeof(LogActionAttribute))
        { }

        private class LogActionAttribute : IAsyncActionFilter
        {
            private readonly TelemetryScopedLogger logger;
            private readonly PlanUsageLogic planUsageLogic;

            public LogActionAttribute(TelemetryScopedLogger logger, PlanUsageLogic planUsageLogic)
            {
                this.logger = logger;
                this.planUsageLogic = planUsageLogic;
            }

            public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
            {               
                await next();

                if (context.HttpContext.Request.Method == HttpMethod.Get.Method)
                {
                    planUsageLogic.LogControlApiGetEvent();
                }
                else
                {
                    planUsageLogic.LogControlApiUpdateEvent();
                }
            }
        }
    }
}