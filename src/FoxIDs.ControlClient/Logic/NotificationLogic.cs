using System;
using System.Threading.Tasks;

namespace FoxIDs.Client.Logic
{
    public class NotificationLogic
    {

        public async Task TenantUpdatedAsync()
        {
            if(OnTenantUpdatedAsync != null)
            {
                await OnTenantUpdatedAsync();
            }
        }

        public event Func<Task> OnTenantUpdatedAsync;
    }
}
