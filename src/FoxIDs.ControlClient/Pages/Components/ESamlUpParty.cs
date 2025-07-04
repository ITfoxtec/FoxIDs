﻿using FoxIDs.Client.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FoxIDs.Infrastructure;
using FoxIDs.Models.Api;
using FoxIDs.Client.Services;
using Microsoft.AspNetCore.Components.Forms;
using ITfoxtec.Identity.BlazorWebAssembly.OpenidConnect;
using FoxIDs.Client.Infrastructure.Security;
using ITfoxtec.Identity;
using System.IO;
using Microsoft.AspNetCore.Components;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using System.Net.Http;
using FoxIDs.Util;
using Microsoft.AspNetCore.Components.Web;

namespace FoxIDs.Client.Pages.Components
{
    public partial class ESamlUpParty : UpPartyBase
    {
        [Inject]
        public HelpersService HelpersService { get; set; }

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            if (!UpParty.CreateMode)
            {
                await DefaultLoadAsync();
            }
        }

        private async Task DefaultLoadAsync()
        {
            try
            {
                var generalSamlUpParty = UpParty as GeneralSamlUpPartyViewModel;
                var samlUpParty = await UpPartyService.GetSamlUpPartyAsync(UpParty.Name);
                await generalSamlUpParty.Form.InitAsync(ToViewModel(generalSamlUpParty, samlUpParty));
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (HttpRequestException ex)
            {
                UpParty.Error = ex.Message;
            }
        }

        private SamlUpPartyViewModel ToViewModel(GeneralSamlUpPartyViewModel generalSamlUpParty, SamlUpParty samlUpParty)
        {
            return samlUpParty.Map<SamlUpPartyViewModel>(afterMap =>
            {
                afterMap.InitName = afterMap.Name;
                if (afterMap.Profiles?.Count() > 0)
                {
                    foreach (var profile in afterMap.Profiles)
                    {
                        profile.InitName = profile.Name;
                    }
                }

                if (afterMap.DisplayName.IsNullOrWhiteSpace())
                {
                    afterMap.DisplayName = afterMap.Name;
                }

                if (samlUpParty.UpdateState == PartyUpdateStates.Manual)
                {
                    afterMap.IsManual = true;
                }

                if (samlUpParty.UpdateState == PartyUpdateStates.AutomaticStopped)
                {
                    afterMap.AutomaticStopped = true;
                }
                else
                {
                    afterMap.AutomaticStopped = false;
                }

                if (samlUpParty.AuthnContextComparison.HasValue)
                {
                    afterMap.AuthnContextComparisonViewModel = (SamlAuthnContextComparisonTypesVievModel)Enum.Parse(typeof(SamlAuthnContextComparisonTypesVievModel), samlUpParty.AuthnContextComparison.Value.ToString());
                }
                else
                {
                    afterMap.AuthnContextComparisonViewModel = SamlAuthnContextComparisonTypesVievModel.Null;
                }

                generalSamlUpParty.KeyInfoList.Clear();
                foreach (var key in afterMap.Keys)
                {
                    generalSamlUpParty.KeyInfoList.Add(new KeyInfoViewModel
                    {
                        Subject = key.CertificateInfo.Subject,
                        ValidFrom = key.CertificateInfo.ValidFrom,
                        ValidTo = key.CertificateInfo.ValidTo,
                        IsValid = key.CertificateInfo.IsValid(),
                        Thumbprint = key.CertificateInfo.Thumbprint,
                        Key = key
                    });
                }

                if (afterMap.ClaimTransforms?.Count > 0)
                {
                    afterMap.ClaimTransforms = afterMap.ClaimTransforms.MapSamlClaimTransforms();
                }

                afterMap.ExtendedUis.MapExtendedUis();

                if (afterMap.ExitClaimTransforms?.Count > 0)
                {
                    afterMap.ExitClaimTransforms = afterMap.ExitClaimTransforms.MapOAuthClaimTransforms();
                }
                if (afterMap.LinkExternalUser?.ClaimTransforms?.Count > 0)
                {
                    afterMap.LinkExternalUser.ClaimTransforms = afterMap.LinkExternalUser.ClaimTransforms.MapOAuthClaimTransforms();
                }

                if(samlUpParty.Profiles?.Count() > 0)
                {
                    foreach (var profile in samlUpParty.Profiles)
                    {
                        var afterMapProfile = afterMap.Profiles.Where(p => p.Name.Equals(profile.Name)).First();
                        if (profile.AuthnContextComparison.HasValue)
                        {
                            afterMapProfile.AuthnContextComparisonViewModel = (SamlAuthnContextComparisonTypesVievModel)Enum.Parse(typeof(SamlAuthnContextComparisonTypesVievModel), profile.AuthnContextComparison.Value.ToString());
                        }
                        else
                        {
                            afterMapProfile.AuthnContextComparisonViewModel = SamlAuthnContextComparisonTypesVievModel.Null;
                        }
                    }
                }
            });
        }

        private async Task SamlUpPartyViewModelAfterInitAsync(GeneralSamlUpPartyViewModel samlUpParty, SamlUpPartyViewModel model)
        {
            if (samlUpParty.CreateMode)
            {
                model.Name = await UpPartyService.GetNewPartyNameAsync();
                if (samlUpParty.TokenExchange)
                {
                    model.DisableUserAuthenticationTrust = true;
                }
                model.Claims = new List<string> { "*" };
                model.DisableLoginHint = true;
                model.IdPInitiatedGrantLifetime = 30;
            }
        }

        private async Task OnReadMetadataFileAsync(GeneralSamlUpPartyViewModel generalSamlUpParty, InputFileChangeEventArgs e)
        {
            generalSamlUpParty.Form.ClearError();
            try
            {
                byte[] metadataXmlBytes;
                using (var memoryStream = new MemoryStream())
                {
                    using var fileStream = e.File.OpenReadStream();
                    await fileStream.CopyToAsync(memoryStream);
                    metadataXmlBytes = memoryStream.ToArray();
                }
                var metadataXml = Encoding.ASCII.GetString(metadataXmlBytes);

                var samlUpParty = await UpPartyService.ReadSamlUpPartyMetadataAsync(new SamlReadMetadataRequest { Type = SamlReadMetadataType.Xml, Metadata = metadataXml });

                generalSamlUpParty.Form.Model.Issuer = samlUpParty.Issuer;
                generalSamlUpParty.Form.Model.AuthnUrl = samlUpParty.AuthnUrl;
                if (samlUpParty.AuthnRequestBinding.HasValue)
                {
                    generalSamlUpParty.Form.Model.AuthnRequestBinding = samlUpParty.AuthnRequestBinding.Value;
                }

                generalSamlUpParty.Form.Model.LogoutUrl = samlUpParty.LogoutUrl;
                if (!string.IsNullOrEmpty(samlUpParty.SingleLogoutResponseUrl))
                {
                    generalSamlUpParty.Form.Model.SingleLogoutResponseUrl = samlUpParty.SingleLogoutResponseUrl;
                }
                if (samlUpParty.LogoutRequestBinding.HasValue)
                {
                    generalSamlUpParty.Form.Model.LogoutRequestBinding = samlUpParty.LogoutRequestBinding.Value;
                }
    
                generalSamlUpParty.KeyInfoList = new List<KeyInfoViewModel>();
                generalSamlUpParty.Form.Model.Keys = new List<JwkWithCertificateInfo>();

                if (samlUpParty.Keys?.Count() > 0)
                {
                    foreach(var key in samlUpParty.Keys)
                    {
                        generalSamlUpParty.KeyInfoList.Add(new KeyInfoViewModel
                        {
                            Subject = key.CertificateInfo.Subject,
                            ValidFrom = key.CertificateInfo.ValidFrom,
                            ValidTo = key.CertificateInfo.ValidTo,
                            IsValid = key.CertificateInfo.IsValid(),
                            Thumbprint = key.CertificateInfo.Thumbprint,
                            Key = key
                        });
                        generalSamlUpParty.Form.Model.Keys.Add(key);
                    }
                }

                generalSamlUpParty.Form.Model.SignAuthnRequest = samlUpParty.SignAuthnRequest;
            }
            catch (Exception ex)
            {
                generalSamlUpParty.Form.SetError($"Failing SAML 2.0 metadata. {ex.Message}");
            }
        }

        private async Task OnSamlUpPartyCertificateFileSelectedAsync(GeneralSamlUpPartyViewModel generalSamlUpParty, InputFileChangeEventArgs e)
        {
            if (generalSamlUpParty.Form.Model.Keys == null)
            {
                generalSamlUpParty.Form.Model.Keys = new List<JwkWithCertificateInfo>();
            }
            generalSamlUpParty.Form.ClearFieldError(nameof(generalSamlUpParty.Form.Model.Keys));
            foreach (var file in e.GetMultipleFiles())
            {
                if (file.Size > GeneralSamlUpPartyViewModel.CertificateMaxFileSize)
                {
                    generalSamlUpParty.Form.SetFieldError(nameof(generalSamlUpParty.Form.Model.Keys), $"That's too big. Max size: {GeneralSamlUpPartyViewModel.CertificateMaxFileSize} bytes.");
                    return;
                }

                generalSamlUpParty.CertificateFileStatus = "Loading...";

                using (var memoryStream = new MemoryStream())
                {
                    using var fileStream = file.OpenReadStream();
                    await fileStream.CopyToAsync(memoryStream);

                    try
                    {
                        var base64UrlEncodeCertificate = WebEncoders.Base64UrlEncode(memoryStream.ToArray());
                        var jwkWithCertificateInfo = await HelpersService.ReadCertificateAsync(new CertificateAndPassword { EncodeCertificate = base64UrlEncodeCertificate });

                        if (generalSamlUpParty.Form.Model.Keys.Any(k => k.Kid.Equals(jwkWithCertificateInfo.Kid, StringComparison.OrdinalIgnoreCase)))
                        {
                            generalSamlUpParty.Form.SetFieldError(nameof(generalSamlUpParty.Form.Model.Keys), "Signature validation keys (certificates) has duplicates.");
                            return;
                        }

                        generalSamlUpParty.KeyInfoList.Add(new KeyInfoViewModel
                        {
                            Subject = jwkWithCertificateInfo.CertificateInfo.Subject,
                            ValidFrom = jwkWithCertificateInfo.CertificateInfo.ValidFrom,
                            ValidTo = jwkWithCertificateInfo.CertificateInfo.ValidTo,
                            IsValid = jwkWithCertificateInfo.CertificateInfo.IsValid(),
                            Thumbprint = jwkWithCertificateInfo.CertificateInfo.Thumbprint,
                            Key = jwkWithCertificateInfo
                        });
                        generalSamlUpParty.Form.Model.Keys.Add(jwkWithCertificateInfo);
                    }
                    catch (Exception ex)
                    {
                        generalSamlUpParty.Form.SetFieldError(nameof(generalSamlUpParty.Form.Model.Keys), ex.Message);
                    }
                }

                generalSamlUpParty.CertificateFileStatus = GeneralSamlUpPartyViewModel.DefaultCertificateFileStatus;
            }
        }

        private void RemoveSamlUpPartyCertificate(GeneralSamlUpPartyViewModel generalSamlUpParty, KeyInfoViewModel keyInfo)
        {
            generalSamlUpParty.Form.ClearFieldError(nameof(generalSamlUpParty.Form.Model.Keys));
            if (generalSamlUpParty.Form.Model.Keys.Remove(keyInfo.Key))
            {
                generalSamlUpParty.KeyInfoList.Remove(keyInfo);
            }
        }

        private void AddProfile(MouseEventArgs e, List<SamlUpPartyProfileViewModel> profiles)
        {
            var profile = new SamlUpPartyProfileViewModel
            {
                Name = RandomName.GenerateDefaultName(profiles.Select(p => p.Name))
            };
            profiles.Add(profile);
        }

        private void RemoveProfile(MouseEventArgs e, List<SamlUpPartyProfileViewModel> profiles, SamlUpPartyProfileViewModel removeProfile)
        {
            profiles.Remove(removeProfile);
        }

        private async Task OnEditSamlUpPartyValidSubmitAsync(GeneralSamlUpPartyViewModel generalSamlUpParty, EditContext editContext)
        {
            try
            {
                generalSamlUpParty.Form.Model.ClaimTransforms.MapSamlClaimTransformsBeforeMap();
                generalSamlUpParty.Form.Model.ExtendedUis.MapExtendedUisBeforeMap();
                generalSamlUpParty.Form.Model.ExitClaimTransforms.MapOAuthClaimTransformsBeforeMap();
                generalSamlUpParty.Form.Model.LinkExternalUser?.ClaimTransforms.MapOAuthClaimTransformsBeforeMap();

                var samlUpParty = generalSamlUpParty.Form.Model.Map<SamlUpParty>(afterMap =>
                {
                    if (generalSamlUpParty.Form.Model.AuthnContextComparisonViewModel != SamlAuthnContextComparisonTypesVievModel.Null)
                    {
                        afterMap.AuthnContextComparison = (SamlAuthnContextComparisonTypes)Enum.Parse(typeof(SamlAuthnContextComparisonTypes), generalSamlUpParty.Form.Model.AuthnContextComparisonViewModel.ToString());
                    }                    

                    if (generalSamlUpParty.Form.Model.IsManual)
                    {
                        afterMap.UpdateState = PartyUpdateStates.Manual;

                    }
                    else
                    {
                        afterMap.UpdateState = PartyUpdateStates.Automatic;
                    }

                    afterMap.ClaimTransforms.MapSamlClaimTransformsAfterMap();
                    afterMap.ExtendedUis.MapExtendedUisAfterMap();
                    afterMap.ExitClaimTransforms.MapOAuthClaimTransformsAfterMap();
                    afterMap.LinkExternalUser = afterMap.LinkExternalUser.MapLinkExternalUserAfterMap();

                    if (!afterMap.EnableIdPInitiated || !(afterMap.IdPInitiatedGrantLifetime > 0))
                    {
                        afterMap.IdPInitiatedGrantLifetime = null;
                    }

                    if (generalSamlUpParty.Form.Model.Profiles?.Count() > 0)
                    {
                        foreach (var profile in generalSamlUpParty.Form.Model.Profiles)
                        {
                            if (profile.AuthnContextComparisonViewModel != SamlAuthnContextComparisonTypesVievModel.Null)
                            {
                                var afterMapProfile = afterMap.Profiles.Where(p => p.Name.Equals(profile.Name)).First();
                                afterMapProfile.AuthnContextComparison = (SamlAuthnContextComparisonTypes)Enum.Parse(typeof(SamlAuthnContextComparisonTypes), profile.AuthnContextComparisonViewModel.ToString());
                            }
                        }
                    }
                });

                if (generalSamlUpParty.CreateMode)
                {
                    var samlUpPartyResult = await UpPartyService.CreateSamlUpPartyAsync(samlUpParty);
                    generalSamlUpParty.Form.UpdateModel(ToViewModel(generalSamlUpParty, samlUpPartyResult));
                    generalSamlUpParty.CreateMode = false;
                    toastService.ShowSuccess("SAML 2.0 application created.");
                    generalSamlUpParty.Name = samlUpPartyResult.Name;
                    generalSamlUpParty.DisplayName = samlUpPartyResult.DisplayName;
                    generalSamlUpParty.Profiles = samlUpPartyResult.Profiles?.Map<List<UpPartyProfile>>();
                }
                else
                {
                    if (generalSamlUpParty.Form.Model.Name != generalSamlUpParty.Form.Model.InitName)
                    {
                        samlUpParty.NewName = samlUpParty.Name;
                        samlUpParty.Name = generalSamlUpParty.Form.Model.InitName;
                    }
                    if (generalSamlUpParty.Form.Model.Profiles?.Count() > 0)
                    {
                        foreach (var profile in generalSamlUpParty.Form.Model.Profiles)
                        {
                            if (!profile.InitName.IsNullOrWhiteSpace() && profile.InitName != profile.Name)
                            {
                                var profileMap = samlUpParty.Profiles?.Where(p => p.Name == profile.Name).First();
                                profileMap.Name = profile.InitName;
                                profileMap.NewName = profile.Name;
                            }
                        }
                    }

                    var samlUpPartyResult = await UpPartyService.UpdateSamlUpPartyAsync(samlUpParty);
                    generalSamlUpParty.Form.UpdateModel(ToViewModel(generalSamlUpParty, samlUpPartyResult));
                    toastService.ShowSuccess("SAML 2.0 application updated.");
                    generalSamlUpParty.Name = samlUpPartyResult.Name;
                    generalSamlUpParty.DisplayName = samlUpPartyResult.DisplayName;
                    generalSamlUpParty.Profiles = samlUpPartyResult.Profiles?.Map<List<UpPartyProfile>>();
                }
            }
            catch (FoxIDsApiException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    generalSamlUpParty.Form.SetFieldError(nameof(generalSamlUpParty.Form.Model.Name), ex.Message);
                }
                else
                {
                    throw;
                }
            }
        }

        private async Task DeleteSamlUpPartyAsync(GeneralSamlUpPartyViewModel generalSamlUpParty)
        {
            try
            {
                await UpPartyService.DeleteSamlUpPartyAsync(generalSamlUpParty.Name);
                UpParties.Remove(generalSamlUpParty);
                await OnStateHasChanged.InvokeAsync(UpParty);
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                generalSamlUpParty.Form.SetError(ex.Message);
            }
        }
    }
}
