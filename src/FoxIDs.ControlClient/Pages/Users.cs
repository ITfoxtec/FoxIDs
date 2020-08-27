using FoxIDs.Client.Infrastructure;
using FoxIDs.Client.Infrastructure.Security;
using FoxIDs.Client.Models.ViewModels;
using FoxIDs.Client.Services;
using FoxIDs.Client.Shared.Components;
using FoxIDs.Models.Api;
using ITfoxtec.Identity.Util;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Authentication;
using System.Threading.Tasks;

namespace FoxIDs.Client.Pages
{
    public partial class Users
    {
        private PageEditForm<FilterUserViewModel> userFilterForm;
        private List<GeneralUserViewModel> users = new List<GeneralUserViewModel>();

        [Inject]
        public UserService UserService { get; set; }

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
                SetGeneralUpParties(await UserService.FilterUserAsync(null));
            }
            catch (AuthenticationException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                userFilterForm.SetError(ex.Message);
            }
        }


        private async Task OnUserFilterValidSubmitAsync(EditContext editContext)
        {
            try
            {
                SetGeneralUpParties(await UserService.FilterUserAsync(userFilterForm.Model.FilterEmail));
            }
            catch (FoxIDsApiException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    userFilterForm.SetFieldError(nameof(userFilterForm.Model.FilterEmail), ex.Message);
                }
                else
                {
                    throw;
                }
            }
        }

        private void SetGeneralUpParties(IEnumerable<User> dataUsers)
        {
            users.Clear();
            foreach (var dp in dataUsers)
            {
                users.Add(new GeneralUserViewModel(dp));
            }
        }

        private void ShowCreateUser()
        {
            var user = new GeneralUserViewModel();
            user.CreateMode = true;
            user.Edit = true;
            users.Insert(0, user);
        }

        private async Task ShowUpdateUserAsync(GeneralUserViewModel generalUser)
        {
            generalUser.CreateMode = false;
            generalUser.DeleteAcknowledge = false;
            generalUser.ShowAdvanced = false;
            generalUser.Error = null;
            generalUser.Edit = true;

            try
            {
                var user = await UserService.GetUserAsync(generalUser.Email);
                await generalUser.Form.InitAsync(user.Map<UserViewModel>(), afterInit: afterInit => 
                {
                    afterInit.Password = "****";
                });
            }
            catch (AuthenticationException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (HttpRequestException ex)
            {
                generalUser.Error = ex.Message;
            }
        }

        private string UserInfoText(GeneralUserViewModel generalUser)
        {
            return $"User - {generalUser.Email}";
        }

        private void UserCancel(GeneralUserViewModel user)
        {
            if (user.CreateMode)
            {
                users.Remove(user);
            }
            else
            {
                user.Edit = false;
            }
        }

        private void AddClaim(MouseEventArgs e, List<ClaimAndValues> claims)
        {
            claims.Add(new ClaimAndValues());
        }

        private void RemoveClaim(MouseEventArgs e, List<ClaimAndValues> claims, ClaimAndValues claimAndValues)
        {
            claims.Remove(claimAndValues);
        }

        private async Task OnEditUserValidSubmitAsync(GeneralUserViewModel generalUser, EditContext editContext)
        {
            try
            {
                if (generalUser.CreateMode)
                {
                    await UserService.CreateUserAsync(generalUser.Form.Model.Map<CreateUserRequest>());
                }
                else
                {
                    await UserService.UpdateUserAsync(generalUser.Form.Model.Map<UserRequest>());
                }
                generalUser.Email = generalUser.Form.Model.Email;
                generalUser.Edit = false;
            }
            catch (FoxIDsApiException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    generalUser.Form.SetFieldError(nameof(generalUser.Form.Model.Email), ex.Message);
                }
                else
                {
                    throw;
                }
            }
        }

        private async Task DeleteUserAsync(GeneralUserViewModel generalUser)
        {
            try
            {
                await UserService.DeleteUserAsync(generalUser.Email);
                users.Remove(generalUser);
            }
            catch (AuthenticationException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                generalUser.Form.SetError(ex.Message);
            }
        }
    }
}
