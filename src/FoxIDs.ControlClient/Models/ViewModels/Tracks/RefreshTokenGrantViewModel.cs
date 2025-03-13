using FoxIDs.Models.Api;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public class RefreshTokenGrantViewModel : RefreshTokenGrant
    {
        [Display(Name = "Expire at")]
        public string ExpireAtText
        {
            get
            {
                return ExpireAt.HasValue ? ExpireAt.Value.ToString() : string.Empty;
            }
            set { }
        }
    }
}
