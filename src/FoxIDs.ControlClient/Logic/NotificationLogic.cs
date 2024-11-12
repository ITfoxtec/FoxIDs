using System;
using System.Threading.Tasks;

namespace FoxIDs.Client.Logic
{
    public class NotificationLogic
    {
        public void ClientSettingLoaded()
        {
            if (OnClientSettingLoaded != null)
            {
                OnClientSettingLoaded();
            }
        }
        public event Action OnClientSettingLoaded;

        public async Task TenantUpdatedAsync()
        {
            if(OnTenantUpdatedAsync != null)
            {
                await OnTenantUpdatedAsync();
            }
        }
        public event Func<Task> OnTenantUpdatedAsync;

        public async Task OpenPaymentMethodAsync()
        {
            if(OnOpenPaymentMethodAsync != null)
            {
                await OnOpenPaymentMethodAsync();
            }
        }
        public event Func<Task> OnOpenPaymentMethodAsync;

        public void RequestPaymentUpdated()
        {
            if (OnRequestPaymentUpdated != null)
            {
                OnRequestPaymentUpdated();
            }
        }
        public event Action OnRequestPaymentUpdated;
    }
}
