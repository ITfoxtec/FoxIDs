﻿using FoxIDs.Models.Logic;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace FoxIDs.Models.Sequences
{
    public class ExternalLoginUpSequenceData : UpSequenceData, ILoginUpSequenceDataBase
    {
        public ExternalLoginUpSequenceData() : base() { }

        public ExternalLoginUpSequenceData(ILoginRequest loginRequest) : base(loginRequest) { }

        [JsonProperty(PropertyName = "si")]
        public string SessionId { get; set; }

        [JsonProperty(PropertyName = "lc")]
        public bool RequireLogoutConsent { get; set; }

        [JsonProperty(PropertyName = "lr")]
        public bool PostLogoutRedirect { get; set; }

        [JsonProperty(PropertyName = "srl")]
        public bool DoSessionUserRequireLogin { get; set; }

        [JsonProperty(PropertyName = "e")]
        public string Email { get; set; }

        [JsonProperty(PropertyName = "c")]
        public IEnumerable<ClaimAndValues> Claims { get; set; }

        [JsonProperty(PropertyName = "am")]
        public IEnumerable<string> AuthMethods { get; set; }

        [JsonProperty(PropertyName = "fst")]
        public TwoFactorAppSequenceStates TwoFactorAppState { get; set; }

        [JsonProperty(PropertyName = "fas")]
        public string TwoFactorAppSecret { get; set; }

        [JsonProperty(PropertyName = "fns")]
        public string TwoFactorAppNewSecret { get; set; }

        [JsonProperty(PropertyName = "frc")]
        public string TwoFactorAppRecoveryCode { get; set; }
    }
}
