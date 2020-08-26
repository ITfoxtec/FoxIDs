using FoxIDs.Client.Infrastructure;
using FoxIDs.Client.Infrastructure.Security;
using FoxIDs.Client.Models.ViewModels;
using FoxIDs.Client.Services;
using FoxIDs.Client.Shared.Components;
using FoxIDs.Models.Api;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using System;
using System.Collections.Generic;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;
using Tewr.Blazor.FileReader;

namespace FoxIDs.Client.Pages
{
    public partial class RiskPasswords
    {
        private ElementReference inputTypeFileElement;
        private string riskPasswordLoadError;
        private GeneralUploadRiskPasswordViewModel uploadRiskPassword { get; set; }
        private PageEditForm<TestRiskPasswordViewModel> testRiskPasswordForm { get; set; }

        [Inject]
        public RiskPasswordService RiskPasswordService { get; set; }

        [Inject]
        public IFileReaderService fileReaderService { get; set; }

        [Parameter]
        public string TenantName { get; set; }

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            await DefaultLoadAsync();
        }

        private async Task DefaultLoadAsync()
        {
            riskPasswordLoadError = null;
            try
            {
                uploadRiskPassword = new GeneralUploadRiskPasswordViewModel(await RiskPasswordService.GetRiskPasswordInfoAsync());
            }
            catch (AuthenticationException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                riskPasswordLoadError = ex.Message;
            }
        }

        private void UploadRiskPasswords(MouseEventArgs e)
        {
            uploadRiskPassword.Edit = true;
        }

        private void UploadRiskPasswordStop()
        {
            uploadRiskPassword.UploadState = GeneralUploadRiskPasswordViewModel.UploadStates.Done;
        }

        private void UploadRiskPasswordClose()
        {
            uploadRiskPassword.Edit = false;
            uploadRiskPassword.UploadState = GeneralUploadRiskPasswordViewModel.UploadStates.Init;
        }

        private void UploadRiskPasswordViewModelAfterInit(UploadRiskPasswordViewModel model)
        {
            model.UploadCount = 0;
            model.File = null;
            uploadRiskPassword.UploadState = GeneralUploadRiskPasswordViewModel.UploadStates.Init;
        }

        private void OnUploadRiskPasswordFileSelected()
        {
            uploadRiskPassword.CertificateFileStatus = "Pwned passwords file selected";
            uploadRiskPassword.UploadState = GeneralUploadRiskPasswordViewModel.UploadStates.Ready;
        }

        public async Task OnUploadRiskPasswordValidSubmitAsync(EditContext editContext)
        {
            uploadRiskPassword.Form.ClearFieldError(nameof(uploadRiskPassword.Form.Model.File));

            foreach (var file in await fileReaderService.CreateReference(inputTypeFileElement).EnumerateFilesAsync())
            {
                var fileInfo = await file.ReadFileInfoAsync();
                if (fileInfo.Size > GeneralUploadRiskPasswordViewModel.CertificateMaxFileSize)
                {
                    uploadRiskPassword.Form.SetFieldError(nameof(uploadRiskPassword.Form.Model.File), $"That's too big. Max size: {GeneralUploadRiskPasswordViewModel.CertificateMaxFileSize} bytes.");
                    return;
                }

                uploadRiskPassword.UploadState = GeneralUploadRiskPasswordViewModel.UploadStates.Active;
                StateHasChanged();
                var riskPasswords = new List<RiskPassword>();
                byte[] buffer = new byte[131072];
                string text = string.Empty;
                await using (var stream = await file.OpenReadAsync())
                {
                    while (uploadRiskPassword.UploadState == GeneralUploadRiskPasswordViewModel.UploadStates.Active)
                    {
                        if ((await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            text += Encoding.ASCII.GetString(buffer);
                            var lineSplit = text.Split(Environment.NewLine);
                            var lineCount = 0;
                            foreach (var line in lineSplit)
                            {
                                lineCount++;
                                if (lineCount < lineSplit.Length)
                                {
                                    var split = line.Split(':');
                                    var passwordCount = Convert.ToInt32(split[1]);
                                    if (passwordCount >= GeneralUploadRiskPasswordViewModel.RiskPasswordMoreThenCount)
                                    {
                                        riskPasswords.Add(new RiskPassword { PasswordSha1Hash = split[0], Count = passwordCount });
                                        if (riskPasswords.Count >= GeneralUploadRiskPasswordViewModel.UploadRiskPasswordBlokCount)
                                        {
                                            uploadRiskPassword.Form.Model.UploadCount += GeneralUploadRiskPasswordViewModel.UploadRiskPasswordBlokCount;
                                            StateHasChanged();
                                            await RiskPasswordService.UpdateUserAsync(new RiskPasswordRequest { RiskPasswords = riskPasswords });
                                            riskPasswords = new List<RiskPassword>();
                                        }
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
                                else
                                {
                                    text = line;
                                }
                            }
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (uploadRiskPassword.UploadState == GeneralUploadRiskPasswordViewModel.UploadStates.Active && riskPasswords.Count > 0)
                    {
                        uploadRiskPassword.Form.Model.UploadCount += riskPasswords.Count;
                        StateHasChanged();
                        await RiskPasswordService.UpdateUserAsync(new RiskPasswordRequest { RiskPasswords = riskPasswords });
                    }
                }
            }
            uploadRiskPassword.UploadState = GeneralUploadRiskPasswordViewModel.UploadStates.Done;
        }

        private async Task OnTestRiskPasswordValidSubmitAsync(EditContext editContext)
        {
            try
            {
                var passwordSha1Hash = testRiskPasswordForm.Model.Password.Sha1Hash();
                var riskPassword = await RiskPasswordService.GetRiskPasswordAsync(passwordSha1Hash);
                if(riskPassword != null)
                {
                    testRiskPasswordForm.Model.IsValid = false;
                }
            }
            catch (FoxIDsApiException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    testRiskPasswordForm.Model.IsValid = true;
                }
                else
                {
                    throw;
                }
            }
        }
    }
}
