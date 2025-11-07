using FoxIDs.Client.Logic;
using ITfoxtec.Identity.BlazorWebAssembly.OpenidConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FoxIDs.Client.Shared
{
    [Authorize]
    public class PageBase : ComponentBase, IDisposable
    {
        private bool isDisposed;
        private CancellationTokenSource cancellationTokenSource;

        [Inject]
        public OpenidConnectPkce OpenidConnectPkce { get; set; }

        [Inject]
        public TrackSelectedLogic TrackSelectedLogic { get; set; }       

        protected CancellationToken PageCancellationToken => cancellationTokenSource?.Token ?? CancellationToken.None;

        protected override Task OnInitializedAsync()
        {
            EnsureCancellationToken();
            return Task.CompletedTask;
        }

        protected void RefreshPageCancellationToken()
        {
            CancelPendingOperations();
            cancellationTokenSource = new CancellationTokenSource();
        }

        private void EnsureCancellationToken()
        {
            if (cancellationTokenSource == null || cancellationTokenSource.IsCancellationRequested)
            {
                cancellationTokenSource?.Dispose();
                cancellationTokenSource = new CancellationTokenSource();
            }
        }

        private void CancelPendingOperations()
        {
            if (cancellationTokenSource == null)
            {
                return;
            }

            try
            {
                if (!cancellationTokenSource.IsCancellationRequested)
                {
                    cancellationTokenSource.Cancel();
                }
            }
            catch (ObjectDisposedException)
            { }
            finally
            {
                cancellationTokenSource.Dispose();
                cancellationTokenSource = null;
            }
        }

        void IDisposable.Dispose()
        {
            if (!isDisposed)
            {
                isDisposed = true;
                CancelPendingOperations();
                OnDispose();
            }
        }

        protected virtual void OnDispose()
        { }
    }
}