using FoxIDs.Models.Api;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public interface ISamlMetadataOrganizationVievModel
    {
        [ValidateComplexType]
        [Display(Name = "Optional organization in metadata")]
        public SamlMetadataOrganization MetadataOrganization { get; set; }
    }
}
