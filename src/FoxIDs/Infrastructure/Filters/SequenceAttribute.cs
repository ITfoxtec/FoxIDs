using FoxIDs.Logic;
using FoxIDs.Models.Sequences;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Threading.Tasks;

namespace FoxIDs.Infrastructure.Filters
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class SequenceAttribute : TypeFilterAttribute
    {
        public SequenceAttribute(SequenceAction action = SequenceAction.Validate) : base(typeof(SequenceActionAttribute))
        {
            Arguments = new object[] { action };
            if (action == SequenceAction.Start)
            {
                Order = 1;
            }
            else
            {
                Order = 2;
            }
        }

        private class SequenceActionAttribute : IAsyncActionFilter
        {
            private readonly SequenceLogic sequenceLogic;
            private readonly SequenceAction action;

            public SequenceActionAttribute(SequenceLogic sequenceLogic, SequenceAction action)
            {
                this.sequenceLogic = sequenceLogic;
                this.action = action;
            }

            public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
            {
                if (action == SequenceAction.Start)
                {
                    await sequenceLogic.StartSequenceAsync(true);
                }
                else if (!context.HttpContext.Items.ContainsKey(Constants.Sequence.Start) && !context.HttpContext.Items.ContainsKey(Constants.Sequence.Valid))
                {
                    var sequenceString = context.HttpContext.GetRouteSequenceString();
                    await sequenceLogic.ValidateAndSetSequenceAsync(sequenceString, true);
                }

                await next();
            }
        }
    }
}
