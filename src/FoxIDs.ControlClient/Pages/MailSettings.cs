using FoxIDs.Client.Infrastructure.Security;
using FoxIDs.Client.Models.ViewModels;
using FoxIDs.Client.Services;
using FoxIDs.Client.Shared.Components;
using FoxIDs.Models.Api;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System;
using ITfoxtec.Identity.BlazorWebAssembly.OpenidConnect;
using System.Threading.Tasks;
using FoxIDs.Client.Logic;
using ITfoxtec.Identity;
using Blazored.Toast.Services;

namespace FoxIDs.Client.Pages
{
    public partial class MailSettings
    {
        private string tenantSettingsHref;
        private string trackSettingsHref;
        private string claimMappingsHref;
        private PageEditForm<MailSettingsViewModel> mailSettingsForm;
        private string deleteMailError;
        private bool deleteMailAcknowledge = false;

        [Inject]
        public IToastService toastService { get; set; }

        [Inject]
        public RouteBindingLogic RouteBindingLogic { get; set; }

        [Inject]
        public TrackService TrackService { get; set; }

        [Parameter]
        public string TenantName { get; set; }

        private bool IsMasterTenant => RouteBindingLogic.IsMasterTenant;

        private bool IsMasterTrack => Constants.Routes.MasterTrackName.Equals(TrackSelectedLogic.Track.Name, StringComparison.OrdinalIgnoreCase);

        protected override async Task OnInitializedAsync()
        {
            tenantSettingsHref = $"{TenantName}/tenantsettings";
            trackSettingsHref = $"{TenantName}/tracksettings";
            claimMappingsHref = $"{TenantName}/claimmappings";
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
                var mailSettings = await TrackService.GetTrackSendEmailAsync();       
                if (mailSettings == null)
                {
                    mailSettings = new SendEmail();
                }
                await mailSettingsForm.InitAsync(mailSettings.Map<MailSettingsViewModel>(afterMap =>
                {
                    afterMap.MailProvider = mailSettings.SmtpHost?.IsNullOrWhiteSpace() == false ? MailProviders.Smtp : MailProviders.SendGrid;
                }));
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                mailSettingsForm.SetError(ex.Message);
            }
        }

        private async Task OnUpdateMailValidSubmitAsync(EditContext editContext)
        {
            try
            {
                if (mailSettingsForm.Model.MailProvider == MailProviders.SendGrid)
                {
                    if(mailSettingsForm.Model.SendgridApiKey.IsNullOrWhiteSpace())
                    {
                        mailSettingsForm.SetFieldError(nameof(mailSettingsForm.Model.SendgridApiKey), "SendGrid key is required.");
                        return;
                    }

                    mailSettingsForm.Model.SmtpHost = null;
                    mailSettingsForm.Model.SmtpPort = 0;
                    mailSettingsForm.Model.SmtpUsername = null;
                    mailSettingsForm.Model.SmtpPassword = null;
                    await TrackService.UpdateTrackSendEmailAsync(new SendEmail
                    {
                        FromEmail = mailSettingsForm.Model.FromEmail,
                        SendgridApiKey = mailSettingsForm.Model.SendgridApiKey
                    });
                }
                else if(mailSettingsForm.Model.MailProvider == MailProviders.Smtp)
                {
                    var smtpOk = true;
                    if (mailSettingsForm.Model.SmtpHost.IsNullOrWhiteSpace())
                    {
                        mailSettingsForm.SetFieldError(nameof(mailSettingsForm.Model.SmtpHost), "SMTP host is required.");
                        smtpOk = false;
                    }
                    if (mailSettingsForm.Model.SmtpPort <= 0)
                    {
                        mailSettingsForm.SetFieldError(nameof(mailSettingsForm.Model.SmtpPort), "SMTP port is required.");
                        smtpOk = false;
                    }
                    if (mailSettingsForm.Model.SmtpUsername.IsNullOrWhiteSpace())
                    {
                        mailSettingsForm.SetFieldError(nameof(mailSettingsForm.Model.SmtpUsername), "SMTP username is required.");
                        smtpOk = false;
                    }
                    if (mailSettingsForm.Model.SmtpPassword.IsNullOrWhiteSpace())
                    {
                        mailSettingsForm.SetFieldError(nameof(mailSettingsForm.Model.SmtpPassword), "SMTP password is required.");
                        smtpOk = false;
                    }

                    if (smtpOk)
                    {

                        mailSettingsForm.Model.SendgridApiKey = null;
                        await TrackService.UpdateTrackSendEmailAsync(new SendEmail
                        {
                            FromEmail = mailSettingsForm.Model.FromEmail,
                            SmtpHost = mailSettingsForm.Model.SmtpHost,
                            SmtpPort = mailSettingsForm.Model.SmtpPort,
                            SmtpUsername = mailSettingsForm.Model.SmtpUsername,
                            SmtpPassword = mailSettingsForm.Model.SmtpPassword
                        });
                    }
                    else
                    {
                        return;
                    }
                }
                else
                {
                    throw new NotImplementedException("Mail provider not implemented.");
                }
                toastService.ShowSuccess("Mail settings updated.");
            }
            catch (Exception ex)
            {
                mailSettingsForm.SetError(ex.Message);
            }
        }

        private async Task DeleteMailAsync()
        {
            try
            {
                await TrackService.DeleteTrackSendEmailAsync();
                deleteMailAcknowledge = false;
                await DefaultLoadAsync();
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                deleteMailError = ex.Message;
            }
        }
    }
}
