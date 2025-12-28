using System.Runtime.Serialization;

namespace FoxIDs.Models.Modules;

public enum NemLoginEnvironments
{
    [EnumMember(Value = "production")]
    Production = 10,

    [EnumMember(Value = "integration_test")]
    IntegrationTest = 20,
}

