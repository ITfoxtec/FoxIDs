using FoxIDs.Infrastructure;
using FoxIDs.Client.Infrastructure.Security;
using ITfoxtec.Identity.BlazorWebAssembly.OpenidConnect;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System;
using System.Diagnostics;
using System.Net.Http;
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
        public EventCallback<TModel> OnAfterInit { get; set; }

        [Parameter]
        public EventCallback<EditContext> OnValidSubmit { get; set; }

        protected override async Task OnInitializedAsync()
        {
            await InitAsync();
            await base.OnInitializedAsync();
        }

        public async Task InitAsync(TModel model = null, Action<TModel> afterInit = null)
        {
            Model = model ?? new TModel();
            await OnAfterInit.InvokeAsync(Model);
            afterInit?.Invoke(Model);
            error = null;
            EditContext = new EditContext(Model);
            validationMessageStore = new ValidationMessageStore(EditContext);
        }

        [Obsolete("delete!")]
        public void Init(TModel model = null, Action<TModel> afterInit = null)
        {
            Model = model ?? new TModel();
            afterInit?.Invoke(Model);
            error = null;
            EditContext = new EditContext(Model);
            validationMessageStore = new ValidationMessageStore(EditContext);
        }

        public void SetError(string error)
        {
            Console.WriteLine(error);
            this.error = error;
            StateHasChanged();
        }

        public void ClearError()
        {
            error = null;
            StateHasChanged();
        }

        public void SetFieldError(string fieldname, string error)
        {
            Console.WriteLine(error);
            validationMessageStore.Add(EditContext.Field(fieldname), error);
            EditContext.NotifyValidationStateChanged();
        }

        public void ClearFieldError(string fieldname)
        {
            validationMessageStore.Clear(EditContext.Field(fieldname));
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
                catch(TokenUnavailableException)
                {
                    await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
                }
                catch (HttpRequestException ex)
                {
                    error = ex.Message;
                }
                catch (FoxIDsApiException aex)
                {
                    error = aex.Message;
                }
            }
            else
            {
                var validationMessages = EditContext.GetValidationMessages();
                foreach(var message  in validationMessages)
                {
                    Console.WriteLine(message);
                }
            }
        }
    }
}
