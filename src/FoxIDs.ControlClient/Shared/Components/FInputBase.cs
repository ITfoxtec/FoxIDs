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
        public Expression<Func<object>> For { get; set; }

        public event Func<TValue, Task> OnValueParsedAsync;

        protected override bool TryParseValueFromString(string value, out TValue result, out string validationErrorMessage)
        {
            if (typeof(TValue) == typeof(string))
            {
                result = (TValue)Convert.ChangeType(value, typeof(TValue));
                validationErrorMessage = null;
                OnValueParsedAsync.Invoke(result);
                return true;
            }
            else if (typeof(TValue) == typeof(int))
            {
                var parseResult = int.TryParse(value, out var intResult);
                if (parseResult)
                {
                    result = (TValue)Convert.ChangeType(intResult, typeof(TValue));
                    validationErrorMessage = null;
                    OnValueParsedAsync.Invoke(result);
                    return true;
                }
                else
                {
                    result = default;
                    validationErrorMessage = $"Unable to pass '{value}' value to int.";
                    return false;
                }
            }
            else if (typeof(TValue) == typeof(bool))
            {
                var parseResult = bool.TryParse(value, out var boolResult);
                if (parseResult)
                {
                    result = (TValue)Convert.ChangeType(boolResult, typeof(TValue));
                    validationErrorMessage = null;
                    OnValueParsedAsync.Invoke(result);
                    return true;
                }
                else
                {
                    result = default;
                    validationErrorMessage = $"Unable to pass '{value}' value to bool.";
                    return false;
                }
            }
            else if (typeof(TValue).IsEnum)
            {
                try
                {
                    result = (TValue)Enum.Parse(typeof(TValue), value);
                    validationErrorMessage = null;
                    OnValueParsedAsync.Invoke(result);
                    return true;
                }
                catch (ArgumentException)
                {
                    result = default;
                    validationErrorMessage = $"Unable to pass '{value}' value to enum.";
                    return false;
                }
            }
            else if (Nullable.GetUnderlyingType(typeof(TValue)).IsEnum)
            {
                try
                {
                    result = (TValue)Enum.Parse(Nullable.GetUnderlyingType(typeof(TValue)), value);
                    validationErrorMessage = null;
                    OnValueParsedAsync.Invoke(result);
                    return true;
                }
                catch (ArgumentException)
                {
                    result = default;
                    validationErrorMessage = $"Unable to pass '{value}' value to enum.";
                    return false;
                }
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
