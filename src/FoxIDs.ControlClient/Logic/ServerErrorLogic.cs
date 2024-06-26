﻿using FoxIDs.Models;
using ITfoxtec.Identity;
using Microsoft.JSInterop;
using System.Threading.Tasks;

namespace FoxIDs.Client.Logic
{
    public class ServerErrorLogic
    {
        private readonly IJSRuntime jsRuntime;
        private bool isLoaded = false;
        private string errorCache;

        public ServerErrorLogic(IJSRuntime jsRuntime)
        {
            this.jsRuntime = jsRuntime;
        }

        public async ValueTask<bool> HasErrorAsync()
        {
            var error = await LoadErrorInternalAsync();
            return !error.IsNullOrEmpty();
        }

        public async ValueTask<ErrorInfo> ReadErrorAsync()
        {
            var error = await LoadErrorInternalAsync();
            return error?.ToObject<ErrorInfo>();
        }

        private async ValueTask<string> LoadErrorInternalAsync()
        {
            if(!isLoaded)
            {
                errorCache = await jsRuntime.InvokeAsync<string>("readError");
                isLoaded = true;
            }
            return errorCache;
        }
    }
}
