using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System;
using System.Threading.Tasks;

namespace FoxIDs.Shared.Components
{
    public partial class PageEditForm<TModel> where TModel : new()
    {
        private string error;

        public EditContext EditContext { get; private set; } = new EditContext(new TModel());
        public TModel Model { get; private set; }

        [Parameter]
        public RenderFragment ChildContent { get; set; }

        [Parameter]
        public EventCallback<EditContext> OnValidSubmit { get; set; }

        protected override void OnInitialized()
        {
            Init();
            base.OnInitialized();
        }

        public void Init()
        {            
            Model = new TModel();
            error = null;
            EditContext = new EditContext(Model);
        }

        public void SetError(string error)
        {
            this.error = error;
            StateHasChanged();
        }

        private async Task OnSubmitAsync()
        {
            error = null;
            var isValid = EditContext.Validate();

            if (isValid)
            {
                try
                {
                    await OnValidSubmit.InvokeAsync(EditContext);
                }
                catch (Exception ex)
                {
                    error = ex.Message;
                }
            }
        }
    }
}
