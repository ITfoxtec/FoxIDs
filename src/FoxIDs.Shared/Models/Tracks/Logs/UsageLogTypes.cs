﻿using System.Runtime.Serialization;

namespace FoxIDs.Models
{
    public enum UsageLogTypes
    {
        [EnumMember(Value = "login")]
        Login = 20,
        [EnumMember(Value = "token_request")]
        TokenRequest = 30,
        [EnumMember(Value = "control_api_get")]
        ControlApiGet = 40,
        [EnumMember(Value = "control_api_update")]
        ControlApiUpdate = 50
    }
}
