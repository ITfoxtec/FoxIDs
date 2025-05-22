using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace FoxIDs.Client.Logic
{
    public class ClipboardLogic
    {
        private readonly IJSRuntime _jsRuntime;

        public ClipboardLogic(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public ValueTask<string> ReadTextAsync()
        {
            return _jsRuntime.InvokeAsync<string>("navigator.clipboard.readText");
        }

        public ValueTask WriteTextAsync(string text)
        {
            return _jsRuntime.InvokeVoidAsync("clipboardWriteText", text);
        }
    }
}
