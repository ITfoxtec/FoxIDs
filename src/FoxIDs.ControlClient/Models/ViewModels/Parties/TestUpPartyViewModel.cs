using FoxIDs.Client.Shared.Components;
using FoxIDs.Models.Api;
using System;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public class TestUpPartyViewModel : UpParty
    {
        public Modal Modal;

        public string Error { get; set; }

        [Display(Name = "Alternatively, start the test with this test URL")]
        public string TestUrl { get; set; }

        public string ExpireAt { get; set; }

        public TestUpPartyViewModel() 
        {
            Type = PartyTypes.Login;
        }
    }
}
