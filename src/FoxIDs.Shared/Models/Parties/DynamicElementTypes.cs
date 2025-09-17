using System.Runtime.Serialization;

namespace FoxIDs.Models
{
    public enum DynamicElementTypes
    {
        [EnumMember(Value = "email")]
        Email = 5,
        [EnumMember(Value = "phone")]
        Phone = 6,
        [EnumMember(Value = "username")]
        Username = 7,

        [EnumMember(Value = "email_password")]
        EmailAndPassword = 10,
        [EnumMember(Value = "password")]
        Password = 11,

        [EnumMember(Value = "name")]
        Name = 20,
        [EnumMember(Value = "given_name")]
        GivenName = 21,
        [EnumMember(Value = "family_name")]
        FamilyName = 22,

        [EnumMember(Value = "text")]
        Text = 200,
        [EnumMember(Value = "html")]
        Html = 210,

        [EnumMember(Value = "custom")]
        Custom = 1000,

        [EnumMember(Value = "login_input")]
        LoginInput = 2010,
        [EnumMember(Value = "login_button")]
        LoginButton = 2020,
        [EnumMember(Value = "login_link")]
        LoginLink = 2030,
        [EnumMember(Value = "login_end")]
        LoginEnd = 2040
    }
}