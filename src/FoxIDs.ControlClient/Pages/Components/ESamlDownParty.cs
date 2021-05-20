using FoxIDs.Client.Models.ViewModels;
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
using System.Security.Cryptography.X509Certificates;
using BlazorInputFile;
using ITfoxtec.Identity.Models;
using System.Security.Claims;

namespace FoxIDs.Client.Pages.Components
{
    public partial class ESamlDownParty : DownPartyBase
    {
        private void SamlDownPartyViewModelAfterInit(GeneralSamlDownPartyViewModel samlDownParty, SamlDownPartyViewModel model)
        {
            if (samlDownParty.CreateMode)
            {
                model.Claims = new List<string> { ClaimTypes.Email, ClaimTypes.Name, ClaimTypes.GivenName, ClaimTypes.Surname };
            }
        }

        private async Task OnSamlDownPartyCertificateFileSelectedAsync(GeneralSamlDownPartyViewModel generalSamlDownParty, IFileListEntry[] files)
        {
            if (generalSamlDownParty.Form.Model.Keys == null)
            {
                generalSamlDownParty.Form.Model.Keys = new List<JsonWebKey>();
            }
            generalSamlDownParty.Form.ClearFieldError(nameof(generalSamlDownParty.Form.Model.Keys));
            foreach (var file in files)
            {
                if (file.Size > GeneralSamlDownPartyViewModel.CertificateMaxFileSize)
                {
                    generalSamlDownParty.Form.SetFieldError(nameof(generalSamlDownParty.Form.Model.Keys), $"That's too big. Max size: {GeneralSamlDownPartyViewModel.CertificateMaxFileSize} bytes.");
                    return;
                }

                generalSamlDownParty.CertificateFileStatus = "Loading...";

                using (var memoryStream = new MemoryStream())
                {
                    await file.Data.CopyToAsync(memoryStream);

                    try
                    {
                        var certificate = new X509Certificate2(memoryStream.ToArray());
                        var jwk = await certificate.ToFTJsonWebKeyAsync();

                        if (generalSamlDownParty.Form.Model.Keys.Any(k => k.X5t.Equals(jwk.X5t, StringComparison.OrdinalIgnoreCase)))
                        {
                            generalSamlDownParty.Form.SetFieldError(nameof(generalSamlDownParty.Form.Model.Keys), "Signature validation keys (certificates) has duplicates.");
                            return;
                        }

                        generalSamlDownParty.KeyInfoList.Add(new KeyInfoViewModel
                        {
                            Subject = certificate.Subject,
                            ValidFrom = certificate.NotBefore,
                            ValidTo = certificate.NotAfter,
                            Thumbprint = certificate.Thumbprint,
                            Key = jwk
                        });
                        generalSamlDownParty.Form.Model.Keys.Add(jwk);
                    }
                    catch (Exception ex)
                    {
                        generalSamlDownParty.Form.SetFieldError(nameof(generalSamlDownParty.Form.Model.Keys), ex.Message);
                    }
                }

                generalSamlDownParty.CertificateFileStatus = GeneralSamlDownPartyViewModel.DefaultCertificateFileStatus;
            }
        }

        private void RemoveSamlDownPartyCertificate(GeneralSamlDownPartyViewModel generalSamlDownParty, KeyInfoViewModel keyInfo)
        {
            generalSamlDownParty.Form.ClearFieldError(nameof(generalSamlDownParty.Form.Model.Keys));
            if (generalSamlDownParty.Form.Model.Keys.Remove(keyInfo.Key))
            {
                generalSamlDownParty.KeyInfoList.Remove(keyInfo);
            }
        }

        private async Task OnEditSamlDownPartyValidSubmitAsync(GeneralSamlDownPartyViewModel generalSamlDownParty, EditContext editContext)
        {
            try
            {
                if (generalSamlDownParty.Form.Model.ClaimTransforms?.Count() > 0)
                {
                    foreach (var claimTransform in generalSamlDownParty.Form.Model.ClaimTransforms)
                    {
                        if (claimTransform is SamlClaimTransformClaimInViewModel claimTransformClaimIn && !claimTransformClaimIn.ClaimIn.IsNullOrWhiteSpace())
                        {
                            claimTransform.ClaimsIn = new List<string> { claimTransformClaimIn.ClaimIn };
                        }
                    }
                }

                var samlDownParty = generalSamlDownParty.Form.Model.Map<SamlDownParty>(afterMap =>
                {
                    afterMap.DisableSignMetadata = !generalSamlDownParty.Form.Model.SignMetadata;

                    if (afterMap.ClaimTransforms?.Count() > 0)
                    {
                        int order = 1;
                        foreach (var claimTransform in afterMap.ClaimTransforms)
                        {
                            claimTransform.Order = order++;
                        }
                    }
                });

                if (generalSamlDownParty.CreateMode)
                {
                    await DownPartyService.CreateSamlDownPartyAsync(samlDownParty);
                }
                else
                {
                    await DownPartyService.UpdateSamlDownPartyAsync(samlDownParty);
                }
                generalSamlDownParty.Name = generalSamlDownParty.Form.Model.Name;
                generalSamlDownParty.Edit = false;
                await OnStateHasChanged.InvokeAsync(DownParty);
            }
            catch (FoxIDsApiException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    generalSamlDownParty.Form.SetFieldError(nameof(generalSamlDownParty.Form.Model.Name), ex.Message);
                }
                else
                {
                    throw;
                }
            }
        }

        private async Task DeleteSamlDownPartyAsync(GeneralSamlDownPartyViewModel generalSamlDownParty)
        {
            try
            {
                await DownPartyService.DeleteSamlDownPartyAsync(generalSamlDownParty.Name);
                DownParties.Remove(generalSamlDownParty);
                await OnStateHasChanged.InvokeAsync(DownParty);
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                generalSamlDownParty.Form.SetError(ex.Message);
            }
        }
    }
}
