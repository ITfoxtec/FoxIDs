using FoxIDs.Client.Infrastructure.Security;
using ITfoxtec.Identity.BlazorWebAssembly.OpenidConnect;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System;
using System.Security.Authentication;
using System.Threading.Tasks;

namespace FoxIDs.Client.Shared.Components
{
    public partial class PageEditForm<TModel> where TModel : class, new()
    {
        private ValidationMessageStore validationMessageStore;
        private string error;

        public EditContext EditContext { get; private set; } = new EditContext(new TModel());
        public TModel Model { get; private set; }

        [Inject]
        public OpenidConnectPkce OpenidConnectPkce { get; set; }

        [Parameter]
        public RenderFragment ChildContent { get; set; }

        [Parameter]
        public EventCallback<EditContext> OnValidSubmit { get; set; }

        protected override void OnInitialized()
        {
            Init();
            base.OnInitialized();
        }

        public void Init(TModel model = null)
        {            
            Model = model ?? new TModel();
            error = null;
            EditContext = new EditContext(Model);
            validationMessageStore = new ValidationMessageStore(EditContext);
        }

        public void SetError(string error)
        {
            this.error = error;
            StateHasChanged();
        }

        public void SetFieldError(string fieldname, string error)
        {
            validationMessageStore.Add(EditContext.Field(fieldname), error);
            EditContext.NotifyValidationStateChanged();
        }

        private async Task OnSubmitAsync()
        {
            error = null;
            validationMessageStore.Clear();
            var isValid = EditContext.Validate();

            if (isValid)
            {
                try
                {
                    await OnValidSubmit.InvokeAsync(EditContext);
                }
                catch(AuthenticationException)
                {
                    await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
                }
                catch (Exception ex)
                {
                    error = ex.Message;
                }
            }
        }
    }
}
