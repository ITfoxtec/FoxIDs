using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public interface IClientSecret
    {
        [Display(Name = "Secrets")]
        List<string> Secrets { get; set; }

        List<OAuthClientSecretViewModel> ExistingSecrets { get; set; }
    }
}
