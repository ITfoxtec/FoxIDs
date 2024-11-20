using FoxIDs.Infrastructure;
using FoxIDs.Client.Infrastructure.Security;
using FoxIDs.Client.Models.ViewModels;
using FoxIDs.Client.Services;
using FoxIDs.Client.Shared.Components;
using FoxIDs.Models.Api;
using ITfoxtec.Identity.BlazorWebAssembly.OpenidConnect;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Blazored.Toast.Services;
using FoxIDs.Client.Logic;

namespace FoxIDs.Client.Pages.Settings
{
    public partial class Plans
    {
        private string trackSettingsHref;
        private string mailSettingsHref;
        private string claimMappingsHref;
        private string textsHref;
        private string riskPasswordsHref;

        private PageEditForm<FilterPlanViewModel> planFilterForm;
        private List<GeneralPlanViewModel> plans = new List<GeneralPlanViewModel>();

        [Inject]
        public IToastService toastService { get; set; }

        [Inject]
        public RouteBindingLogic RouteBindingLogic { get; set; }

        [Inject]
        public PlanService PlanService { get; set; }

        [Parameter]
        public string TenantName { get; set; }

        private bool IsMasterTenant => RouteBindingLogic.IsMasterTenant;

        protected override async Task OnInitializedAsync()
        {
            trackSettingsHref = $"{TenantName}/envsettings";
            mailSettingsHref = $"{TenantName}/mailsettings";
            claimMappingsHref = $"{TenantName}/claimmappings";
            textsHref = $"{TenantName}/texts";
            riskPasswordsHref = $"{TenantName}/riskpasswords";
            await base.OnInitializedAsync();
            TrackSelectedLogic.OnTrackSelectedAsync += OnTrackSelectedAsync;
            if (TrackSelectedLogic.IsTrackSelected)
            {
                await DefaultLoadAsync();
            }
        }

        protected override void OnDispose()
        {
            TrackSelectedLogic.OnTrackSelectedAsync -= OnTrackSelectedAsync;
            base.OnDispose();
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
                await generalPlan.Form.InitAsync(ToViewModel(plan));
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

        private PlanViewModel ToViewModel(Plan plan)
        {
            return plan.Map<PlanViewModel>(afterMap: afterMap =>
            {
                if (!afterMap.LogLifetime.HasValue)
                {
                    afterMap.LogLifetime = LogLifetimeOptionsVievModel.Null;
                }
            });
        }


        private void PlanViewModelAfterInit(GeneralPlanViewModel generalPlan, PlanViewModel plan)
        {
            plan.Users = plan.Users ?? new PlanItem();
            plan.Logins = plan.Logins ?? new PlanItem();
            plan.TokenRequests = plan.TokenRequests ?? new PlanItem();
            plan.ControlApiGetRequests = plan.ControlApiGetRequests ?? new PlanItem();
            plan.ControlApiUpdateRequests = plan.ControlApiUpdateRequests ?? new PlanItem();            
        }

        private string PlanInfoText(GeneralPlanViewModel generalPlan)
        {
            return $"Plan - {PlanDisplayName(generalPlan)}";
        }

        private string PlanDisplayName(GeneralPlanViewModel generalPlan)
        {
            return generalPlan.DisplayName ?? generalPlan.Name;
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
                if (generalPlan.Form.Model.LogLifetime == LogLifetimeOptionsVievModel.Null)
                {
                    generalPlan.Form.Model.LogLifetime = null;
                }

                var plan = generalPlan.Form.Model.Map<Plan>(afterMap: afterMap =>
                {
                
                });

                if (generalPlan.CreateMode)
                {
                    var planResult = await PlanService.CreatePlanAsync(plan);
                    generalPlan.Form.UpdateModel(ToViewModel(planResult));
                    generalPlan.CreateMode = false;
                    toastService.ShowSuccess("Plan created.");
                }
                else
                {
                    var planResult = await PlanService.UpdatePlanAsync(plan);
                    generalPlan.Form.UpdateModel(ToViewModel(planResult));
                    toastService.ShowSuccess("Plan updated.");
                }

                generalPlan.Name = generalPlan.Form.Model.Name;
                generalPlan.DisplayName = generalPlan.Form.Model.DisplayName;
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
