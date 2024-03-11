using FoxIDs.Client.Logic;
using ITfoxtec.Identity.BlazorWebAssembly.OpenidConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using System;
using System.Threading.Tasks;

namespace FoxIDs.Client.Shared
{
    [Authorize]
    public class PageBase : ComponentBase, IDisposable
    {
        private bool isDisposed;

        [Inject]
        public OpenidConnectPkce OpenidConnectPkce { get; set; }

        [Inject]
        public TrackSelectedLogic TrackSelectedLogic { get; set; }

        [Inject]
        public ControlClientSettingLogic ControlClientSettingLogic { get; set; }

        protected override async Task OnInitializedAsync()
        {
            await ControlClientSettingLogic.InitLoadAsync();
        }

        void IDisposable.Dispose()
        {
            if (!isDisposed)
            {
                isDisposed = true;
                OnDispose();
            }
        }

        protected virtual void OnDispose()
        { }
    }
}
