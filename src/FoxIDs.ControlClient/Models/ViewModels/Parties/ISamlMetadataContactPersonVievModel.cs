using FoxIDs.Infrastructure.DataAnnotations;
using FoxIDs.Models.Api;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public interface ISamlMetadataContactPersonVievModel
    {
        [ListLength(Constants.Models.SamlParty.MetadataContactPersonsMin, Constants.Models.SamlParty.MetadataContactPersonsMax)]
        [Display(Name = "Contact persons in metadata")]
        List<SamlMetadataContactPerson> MetadataContactPersons { get; set; }
    }
}
