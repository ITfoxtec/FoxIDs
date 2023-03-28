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
    public partial class Users
    {
        private PageEditForm<FilterUserViewModel> userFilterForm;
        private List<GeneralUserViewModel> users = new List<GeneralUserViewModel>();

        [Inject]
        public IToastService toastService { get; set; }

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
                SetGeneralUsers(await UserService.FilterUserAsync(null));
            }
            catch (TokenUnavailableException)
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
                SetGeneralUsers(await UserService.FilterUserAsync(userFilterForm.Model.FilterEmail));
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

        private void SetGeneralUsers(IEnumerable<User> dataUsers)
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
            users.Add(user);
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
                await generalUser.Form.InitAsync(ToViewModel(user));
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (HttpRequestException ex)
            {
                generalUser.Error = ex.Message;
            }
        }

        private UserViewModel ToViewModel(User user)
        {
            return user.Map<UserViewModel>(afterMap: afterMap =>
            {
                afterMap.Password = "****";
                afterMap.AccountStatus = !user.DisableAccount;
            });
        }


        private void UserViewModelAfterInit(GeneralUserViewModel generalUser, UserViewModel user)
        {
            if (generalUser.CreateMode)
            {
                user.ConfirmAccount = true;
                user.EmailVerified = false;
                user.ChangePassword = true;
                user.AccountStatus = !generalUser.DisableAccount;
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
                    var userResult = await UserService.CreateUserAsync(generalUser.Form.Model.Map<CreateUserRequest>(afterMap: afterMap => afterMap.DisableAccount = !generalUser.Form.Model.AccountStatus));
                    generalUser.Form.UpdateModel(ToViewModel(userResult));
                    generalUser.CreateMode = false;
                    toastService.ShowSuccess("User created.");
                }
                else
                {
                    var userResult = await UserService.UpdateUserAsync(generalUser.Form.Model.Map<UserRequest>(afterMap: afterMap => afterMap.DisableAccount = !generalUser.Form.Model.AccountStatus));
                    generalUser.Form.UpdateModel(ToViewModel(userResult));
                    toastService.ShowSuccess("User updated.");
                }

                generalUser.Email = generalUser.Form.Model.Email;
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
            catch (TokenUnavailableException)
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
