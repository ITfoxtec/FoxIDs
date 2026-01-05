using FoxIDs.Models.Api;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels;

public class SamlUpPartyNemLoginModuleViewModel : SamlUpPartyNemLoginModule, IValidatableObject
{
    public string NemLoginTrackCertificateBase64Url { get; set; }

    public string NemLoginTrackCertificatePassword { get; set; }

    [Display(Name = "Minimum level of assurance (LoA)")]
    public string NemLoginMinimumLoa { get; set; }

    [Display(Name = "Requested attribute profiles")]
    public List<string> NemLoginRequestedAttributeProfiles { get; set; } = new List<string>();

    [Display(Name = "Android app-switch")]
    public bool NemLoginAppSwitchAndroidEnabled { get; set; }

    [Display(Name = "Android app-switch return URL")]
    public string NemLoginAppSwitchAndroidReturnUrl { get; set; }

    [Display(Name = "iOS app-switch")]
    public bool NemLoginAppSwitchIosEnabled { get; set; }

    [Display(Name = "iOS app-switch return URL")]
    public string NemLoginAppSwitchIosReturnUrl { get; set; }

    public string NemLoginTrackCertificateFileStatus { get; set; } = GeneralSamlUpPartyViewModel.DefaultCertificateFileStatus;

    public Guid NemLoginTrackCertificateInputFileKey { get; set; } = Guid.NewGuid();

    public KeyInfoViewModel NemLoginTrackCertificateInfo { get; set; }

    public string NemLoginTrackCertificateError { get; set; }

    public bool NemLoginTrackCertificateEdit { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (NemLoginAppSwitchAndroidEnabled && string.IsNullOrWhiteSpace(NemLoginAppSwitchAndroidReturnUrl))
        {
            yield return new ValidationResult("The Android app-switch return URL field is required.", new[] { nameof(NemLoginAppSwitchAndroidReturnUrl) });
        }
        if (NemLoginAppSwitchIosEnabled && string.IsNullOrWhiteSpace(NemLoginAppSwitchIosReturnUrl))
        {
            yield return new ValidationResult("The iOS app-switch return URL field is required.", new[] { nameof(NemLoginAppSwitchIosReturnUrl) });
        }
    }
}
