using FoxIDs.Infrastructure;
using FoxIDs.Client.Infrastructure.Security;
using ITfoxtec.Identity.BlazorWebAssembly.OpenidConnect;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using ITfoxtec.Identity;

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
        public EventCallback<PageEditForm<TModel>> OnAfterInitialized { get; set; }

        [Parameter]
        public EventCallback<TModel> OnAfterInit { get; set; }

        [Parameter]
        public EventCallback<EditContext> OnValidSubmit { get; set; }

        protected override async Task OnInitializedAsync()
        {
            await InitAsync();
            await OnAfterInitialized.InvokeAsync(this);
            await base.OnInitializedAsync();
        }

        public async Task InitAsync(TModel model = null, Action<TModel> afterInit = null)
        {
            Model = model ?? new TModel();
            await OnAfterInit.InvokeAsync(Model);
            afterInit?.Invoke(Model);
            error = null;
            InitializeValidationState();
        }

        public void Init(TModel model = null, Action<TModel> afterInit = null)
        {
            Model = model ?? new TModel();
            afterInit?.Invoke(Model);
            error = null;
            InitializeValidationState();
        }

        public void UpdateModel(TModel model)
        {
            Model = model;
            error = null;
            InitializeValidationState();
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

        public void SetFieldError(string fieldName, string error)
        {
            Console.WriteLine(error);
            EnsureValidationStateInitialized();
            validationMessageStore.Add(EditContext.Field(fieldName), error);
            EditContext.NotifyValidationStateChanged();
        }

        public void ClearFieldError(string fieldName)
        {
            EnsureValidationStateInitialized();
            validationMessageStore.Clear(EditContext.Field(fieldName));
            EditContext.NotifyValidationStateChanged();
        }

        public async Task<(bool isValid, string error)> Submit()
        {
            error = null;
            EnsureValidationStateInitialized();
            validationMessageStore.Clear();
            var isValid = EditContext.Validate();

            if (isValid)
            {
                try
                {
                    await OnValidSubmit.InvokeAsync(EditContext);
                }
                catch (TokenUnavailableException)
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
                return (isValid, error);
            }
            else
            {
                Console.WriteLine("Form has error.");
                var valError = string.Empty;
                foreach (var message in EditContext.GetValidationMessages())
                {
                    if(valError.IsNullOrWhiteSpace())
                    {
                        valError = message;
                    }
                    Console.WriteLine(message);
                }
                return (isValid, valError);
            }
        }

        private async Task OnSubmitAsync()
        {
            _ = await Submit();
        }

        private void InitializeValidationState()
        {
            EditContext = new EditContext(Model);
            validationMessageStore = new ValidationMessageStore(EditContext);
        }

        private void EnsureValidationStateInitialized()
        {
            if (EditContext == null || validationMessageStore == null)
            {
                InitializeValidationState();
            }
        }
    }
}
