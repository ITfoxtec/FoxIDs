using BlazorInputFile;
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
using System.IO;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;
using Tewr.Blazor.FileReader;

//https://github.com/Tewr/BlazorFileReader
//https://github.com/Tewr/BlazorFileReader/blob/master/src/Demo/Blazor.FileReader.Demo.Common/IndexCommon.razor
//https://tewr.github.io/BlazorFileReader/

namespace FoxIDs.Client.Pages
{
    public partial class RiskPasswords
    {
        private string riskPasswordLoadError;
        private GeneralUploadRiskPasswordViewModel uploadRiskPassword { get; set; }
        private PageEditForm<TestRiskPasswordViewModel> testRiskPasswordForm { get; set; }

        [Inject]
        public RiskPasswordService RiskPasswordService { get; set; }

        [Inject]
        public IFileReaderService fileReaderService { get; set; }

        [Parameter]
        public string TenantName { get; set; }


        private ElementReference inputTypeFileElement;

        public async Task ReadFile()
        {
            foreach (var file in await fileReaderService.CreateReference(inputTypeFileElement).EnumerateFilesAsync())
            {
                //var fileInfo = await file.ReadFileInfoAsync();
                // Read into buffer and act (uses less memory)

                var riskPasswords = new List<RiskPassword>();

                byte[] buffer = new byte[131072]; //131072
                string text = string.Empty;
                await using (var stream = await file.OpenReadAsync())
                {
                    uploadRiskPassword.UploadState = GeneralUploadRiskPasswordViewModel.UploadStates.Active;

                    var totalLineCount = 0;
                    while (true)
                    {
                        //if(totalLineCount > 10000)
                        //{
                        //    break;
                        //}

                        if ((await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            text += Encoding.ASCII.GetString(buffer);
                            var lineSplit = text.Split(Environment.NewLine);//new[] { "\r\n", "\r", "\n" });
                            var lineCount = 0;
                            foreach (var line in lineSplit)
                            {
                                //Console.WriteLine("Line: " + line);
                                lineCount++;
                                if (lineCount < lineSplit.Length)
                                {
                                    var split = line.Split(':');
                                    //Console.WriteLine($"Line split (l:{lineSplit.Length}, c:{lineCount}): " + split[0] + ", " + split[1]);
                                    var passwordCount = Convert.ToInt32(split[1]);
                                    if (passwordCount >= GeneralUploadRiskPasswordViewModel.RiskPasswordMoreThenCount)
                                    {
                                        riskPasswords.Add(new RiskPassword { PasswordSha1Hash = split[0], Count = passwordCount });
                                        if (riskPasswords.Count >= GeneralUploadRiskPasswordViewModel.UploadRiskPasswordBlokCount)
                                        {
                                            await RiskPasswordService.UpdateUserAsync(new RiskPasswordRequest { RiskPasswords = riskPasswords });
                                            uploadRiskPassword.Form.Model.UploadCount += GeneralUploadRiskPasswordViewModel.UploadRiskPasswordBlokCount;
                                            StateHasChanged();
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
                                    //Console.WriteLine("Text before set: " + text);
                                    Console.WriteLine("line to text: " + line);
                                    text = line;
                                    //Console.WriteLine("Text after set: " + text);
                                }
                            }
                            totalLineCount += lineCount;
                        }
                        else
                        {
                            break;
                        }                        
                    }

                    if (riskPasswords.Count > 0)
                    {
                        await RiskPasswordService.UpdateUserAsync(new RiskPasswordRequest { RiskPasswords = riskPasswords });
                        uploadRiskPassword.Form.Model.UploadCount += riskPasswords.Count;
                        StateHasChanged();
                    }



                    //var riskPasswords = new List<RiskPassword>();
                    //using (var streamReader = new StreamReader(stream))
                    //{
                    //    var line = await streamReader.ReadLineAsync();
                    //    while (line != null)
                    //    {
                    //        var split = line.Split(':');
                    //        var passwordCount = Convert.ToInt32(split[1]);
                    //        if (passwordCount >= GeneralUploadRiskPasswordViewModel.RiskPasswordMoreThenCount)
                    //        {
                    //            riskPasswords.Add(new RiskPassword { PasswordSha1Hash = split[0], Count = passwordCount });
                    //            if (riskPasswords.Count >= GeneralUploadRiskPasswordViewModel.UploadRiskPasswordBlokCount)
                    //            {
                    //                await RiskPasswordService.UpdateUserAsync(new RiskPasswordRequest { RiskPasswords = riskPasswords });
                    //                uploadRiskPassword.Form.Model.UploadCount += GeneralUploadRiskPasswordViewModel.UploadRiskPasswordBlokCount;
                    //                riskPasswords = new List<RiskPassword>();
                    //            }
                    //        }
                    //        else
                    //        {
                    //            break;
                    //        }
                    //        line = await streamReader.ReadLineAsync();
                    //    }
                    //}

                    //if (riskPasswords.Count > 0)
                    //{
                    //    await RiskPasswordService.UpdateUserAsync(new RiskPasswordRequest { RiskPasswords = riskPasswords });
                    //    uploadRiskPassword.Form.Model.UploadCount += riskPasswords.Count;
                    //}

                }
            }
            uploadRiskPassword.UploadState = GeneralUploadRiskPasswordViewModel.UploadStates.Done;                    
        }

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
            uploadRiskPassword.UploadState = GeneralUploadRiskPasswordViewModel.UploadStates.Init;
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

        private async Task OnUploadRiskPasswordFileSelected(IFileListEntry[] files)
        {
            uploadRiskPassword.Form.ClearFieldError(nameof(uploadRiskPassword.Form.Model.File));
            foreach (var file in files)
            {
                if (file.Size > GeneralUploadRiskPasswordViewModel.CertificateMaxFileSize)
                {
                    uploadRiskPassword.Form.SetFieldError(nameof(uploadRiskPassword.Form.Model.File), $"That's too big. Max size: {GeneralUploadRiskPasswordViewModel.CertificateMaxFileSize} bytes.");
                    return;
                }

                uploadRiskPassword.Form.Model.File = file;
                uploadRiskPassword.CertificateFileStatus = "Pwned passwords file selected";

                var buffer = new byte[1024];
                await uploadRiskPassword.Form.Model.File.Data.ReadAsync(buffer, 0, 1024);
                uploadRiskPassword.CertificateFileStatus = Convert.ToString(buffer);
                //using (var streamReader = new StreamReader(uploadRiskPassword.Form.Model.File.Data))
                //{
                //    uploadRiskPassword.CertificateFileStatus = await streamReader.ReadLineAsync();
                //}

                uploadRiskPassword.UploadState = GeneralUploadRiskPasswordViewModel.UploadStates.Ready;
                return;
            }
        }

        private async Task OnUploadRiskPasswordValidSubmitAsync(EditContext editContext)
        {
            if(uploadRiskPassword.UploadState != GeneralUploadRiskPasswordViewModel.UploadStates.Ready)
            {
                throw new InvalidOperationException("Upload status not ready.");
            }
            uploadRiskPassword.UploadState = GeneralUploadRiskPasswordViewModel.UploadStates.Active;

            var riskPasswords = new List<RiskPassword>();
            using (var streamReader = new StreamReader(uploadRiskPassword.Form.Model.File.Data))
            {
                var line = await streamReader.ReadLineAsync();
                while (line != null)
                {
                    var split = line.Split(':');
                    var passwordCount = Convert.ToInt32(split[1]);
                    if (passwordCount >= GeneralUploadRiskPasswordViewModel.RiskPasswordMoreThenCount)
                    {
                        riskPasswords.Add(new RiskPassword { PasswordSha1Hash = split[0], Count = passwordCount });
                        if (riskPasswords.Count >= GeneralUploadRiskPasswordViewModel.UploadRiskPasswordBlokCount)
                        {
                            await RiskPasswordService.UpdateUserAsync(new RiskPasswordRequest { RiskPasswords = riskPasswords });
                            uploadRiskPassword.Form.Model.UploadCount += GeneralUploadRiskPasswordViewModel.UploadRiskPasswordBlokCount;
                            riskPasswords = new List<RiskPassword>();
                        }
                    }
                    else
                    {
                        break;
                    }
                    line = await streamReader.ReadLineAsync();
                }
            }

            if (riskPasswords.Count > 0)
            {
                await RiskPasswordService.UpdateUserAsync(new RiskPasswordRequest { RiskPasswords = riskPasswords });
                uploadRiskPassword.Form.Model.UploadCount += riskPasswords.Count;
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
