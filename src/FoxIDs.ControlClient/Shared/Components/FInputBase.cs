using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace FoxIDs.Client.Shared.Components
{
    public class FInputBase<TValue> : InputBase<TValue>
    {
        protected ElementReference inputElement;

        [Inject]
        public IJSRuntime jsRuntime { get; set; }

        [Parameter]
        public bool Focus { get; set; } = false;

        [Parameter]
        public Expression<Func<TValue>> For { get; set; }

        protected override bool TryParseValueFromString(string value, out TValue result, out string validationErrorMessage)
        {
            if (typeof(TValue) == typeof(string))
            {
                result = (TValue)Convert.ChangeType(value, typeof(TValue));
                validationErrorMessage = null;
                return true;
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender && Focus)
            {
                await jsRuntime.InvokeVoidAsync("SetElementFocus", inputElement);
            }
            await base.OnAfterRenderAsync(firstRender);
        }
    }
}
