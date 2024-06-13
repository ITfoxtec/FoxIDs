using FoxIDs.Client.Shared.Components;
using FoxIDs.Models.Api;
using System;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public class TestUpPartyViewModel : UpParty
    {
        public Modal Modal;

        [Display(Name = "Alternatively, start the test with this test URL")]
        public string TestUrl { get; set; }

        public DateTime ExpireAt { get; set; }

        public TestUpPartyViewModel() 
        {
            Type = PartyTypes.Login;
        }
    }
}
