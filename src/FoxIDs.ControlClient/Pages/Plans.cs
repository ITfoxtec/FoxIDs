using FoxIDs.Infrastructure;
using FoxIDs.Client.Infrastructure.Security;
using FoxIDs.Client.Models.ViewModels;
using FoxIDs.Client.Services;
using FoxIDs.Client.Shared.Components;
using FoxIDs.Models.Api;
using ITfoxtec.Identity.BlazorWebAssembly.OpenidConnect;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Blazored.Toast.Services;

namespace FoxIDs.Client.Pages
{
    public partial class Plans
    {
        private PageEditForm<FilterPlanViewModel> planFilterForm;
        private List<GeneralPlanViewModel> plans = new List<GeneralPlanViewModel>();

        [Inject]
        public IToastService toastService { get; set; }

        [Inject]
        public PlanService PlanService { get; set; }

        [Parameter]
        public string TenantName { get; set; }

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            TrackSelectedLogic.OnTrackSelectedAsync += OnTrackSelectedAsync;
            if (TrackSelectedLogic.IsTrackSelected)
            {
                await DefaultLoadAsync();
            }
        }

        private async Task OnTrackSelectedAsync(Track track)
        {
            await DefaultLoadAsync();
            StateHasChanged();
        }

        private async Task DefaultLoadAsync()
        {
            try
            {
                SetGeneralPlans(await PlanService.FilterPlanAsync(null));
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                planFilterForm.SetError(ex.Message);
            }
        }


        private async Task OnPlanFilterValidSubmitAsync(EditContext editContext)
        {
            try
            {
                SetGeneralPlans(await PlanService.FilterPlanAsync(planFilterForm.Model.FilterName));
            }
            catch (FoxIDsApiException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    planFilterForm.SetFieldError(nameof(planFilterForm.Model.FilterName), ex.Message);
                }
                else
                {
                    throw;
                }
            }
        }

        private void SetGeneralPlans(IEnumerable<Plan> dataPlans)
        {
            plans.Clear();
            foreach (var dp in dataPlans)
            {
                plans.Add(new GeneralPlanViewModel(dp));
            }
        }

        private void ShowCreatePlan()
        {
            var plan = new GeneralPlanViewModel();
            plan.CreateMode = true;
            plan.Edit = true;
            plans.Add(plan);
        }

        private async Task ShowUpdatePlanAsync(GeneralPlanViewModel generalPlan)
        {
            generalPlan.CreateMode = false;
            generalPlan.DeleteAcknowledge = false;
            generalPlan.ShowAdvanced = false;
            generalPlan.Error = null;
            generalPlan.Edit = true;

            try
            {
                var plan = await PlanService.GetPlanAsync(generalPlan.Name);
                await generalPlan.Form.InitAsync(plan);
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (HttpRequestException ex)
            {
                generalPlan.Error = ex.Message;
            }
        }

        private void PlanViewModelAfterInit(GeneralPlanViewModel generalPlan, Plan plan)
        {
            if (generalPlan.CreateMode)
            {
                plan.Currency = "EUR";
            }
            plan.Users = plan.Users ?? new PlanItem();
            plan.Logins = plan.Logins ?? new PlanItem();
            plan.TokenRequests = plan.TokenRequests ?? new PlanItem();
            plan.ControlApiGetRequests = plan.ControlApiGetRequests ?? new PlanItem();
            plan.ControlApiUpdateRequests = plan.ControlApiUpdateRequests ?? new PlanItem();
        }

        private string PlanInfoText(GeneralPlanViewModel generalPlan)
        {
            return $"Plan - {generalPlan.Name}";
        }

        private void PlanCancel(GeneralPlanViewModel plan)
        {
            if (plan.CreateMode)
            {
                plans.Remove(plan);
            }
            else
            {
                plan.Edit = false;
            }
        }

        private async Task OnEditPlanValidSubmitAsync(GeneralPlanViewModel generalPlan, EditContext editContext)
        {
            try
            {
                if (generalPlan.CreateMode)
                {
                    var planResult = await PlanService.CreatePlanAsync(generalPlan.Form.Model);
                    generalPlan.Form.UpdateModel(planResult);
                    generalPlan.CreateMode = false;
                    toastService.ShowSuccess("Plan created.");
                }
                else
                {
                    var planResult = await PlanService.UpdatePlanAsync(generalPlan.Form.Model);
                    generalPlan.Form.UpdateModel(planResult);
                    toastService.ShowSuccess("Plan updated.");
                }

                generalPlan.Name = generalPlan.Form.Model.Name;
            }
            catch (FoxIDsApiException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    generalPlan.Form.SetFieldError(nameof(generalPlan.Form.Model.Name), ex.Message);
                }
                else
                {
                    throw;
                }
            }
        }

        private async Task DeletePlanAsync(GeneralPlanViewModel generalPlan)
        {
            try
            {
                await PlanService.DeletePlanAsync(generalPlan.Name);
                plans.Remove(generalPlan);
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                generalPlan.Form.SetError(ex.Message);
            }
        }
    }
}
