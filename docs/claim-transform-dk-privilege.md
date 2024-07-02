# DK privilege - claim transforms

FoxIDs support claim transforms of DK privilege used in Danish [NemLog-in](auth-method-howto-saml-2.0-nemlogin.md) and [Context Handler](howto-saml-2.0-context-Handler.md) IdPs.

Supported privilege standard: 

- [OIO Basic Privilege Profile, Version 1.2](https://digst.dk/media/20999/oiosaml-basic-privilege-profile-1_2.pdf)
- FoxIDs support `PrivilegeGroup` elements defined in [model 2](#model-2) (scoping and delegation) and [model 3](#model-3) (scoping, delegation and constraint).
- FoxIDs support both to read the base64-encoded privilege string from the standard claim `https://data.gov.dk/model/core/eid/privilegesIntermediate` and a custom defined claim.

## Configuring DK privilege - claim transforms
The DK privilege can both be configured in a SAML 2.0 authentication method and application registration and likewise in a OpenID Connect authentication method and application registration.

- In SAML 2.0 the DK privilege claim transformer default read the standard claim `https://data.gov.dk/model/core/eid/privilegesIntermediate` and issue the transformed claim `http://schemas.foxids.com/identity/claims/privilege`.
- In OpenID Connect the DK privilege claim transformer default read the standard claim `privileges_intermediate` and issue the transformed claim `privilege`.

Configure the DK privilege claim transformer on SAML 2.0 authentication method in [FoxIDs Control Client](control.md#foxids-control-client):

1. Select the **Claim transform** tab
2. Click **Add claim transform** and click **DK XML privilege to JSON**
3. Click **Add claim transform** and click **Match claim**
4. As **Action** select **Remove claim**, to remove the original privilege claim from the claims pipeline
5. In the **Remove claim** add `https://data.gov.dk/model/core/eid/privilegesIntermediate` 
6. Click **Update**

![Context Handler SAML 2.0 authentication method privilege claim transformation](images/howto-saml-privilege-claim-tf.png)


> Remember to add a [claim mapping](saml-2.0.md#claim-mappings) from SAML `http://schemas.foxids.com/identity/claims/privilege` to JWT `privilege` in the settings section. If you e.g. use a [SAML 2.0 authentication method](auth-method-saml-2.0.md) and a [OpenID Connect application registration](app-reg-oidc.md).

## Model 2
The DK privilege claim is transformed into a list of claims, one claim for each group. The XML PrivilegeGroup element is transformed into a JSON object and serialized as a string.

The 4 possible scopes are translated into a properties with a short name:
- `Scope="urn:dk:gov:saml:cvrNumberIdentifier:<cvr_number>"` become `"cvr": "<cvr_number>"`
- `Scope="urn:dk:gov:saml:productionUnitIdentifier:<p_number>"` become `"p": "<p_number>"`
- `Scope="urn:dk:gov:saml:seNumberIdentifier:<se_number>"` become `"se": "<se_number>"`
- `Scope="urn:dk:gov:saml:cprNumberIdentifier:<cpr_number>"` become `"cpr": "<cpr_number>"`

The `Privilege` element(s) are translated into the property `p` with the privilege values(s) as a list.

DK privilege base64-decoded sample:  
*(with extra spaces and line breaks for display purposes only)*

    <?xml version="1.0" encoding="UTF-8"?>
    <bpp:PrivilegeList xmlns:bpp="http://digst.dk/oiosaml/basic_privilege_profile" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" >
        <PrivilegeGroup Scope="urn:dk:gov:saml:cvrNumberIdentifier:12345678">
            <Privilege>urn:dk:some_domain:myPrivilege1A</Privilege>
            <Privilege>urn:dk:some_domain:myPrivilege1B</Privilege>
        </PrivilegeGroup>
        <PrivilegeGroup Scope="urn:dk:gov:saml:seNumberIdentifier:27384223">
            <Privilege>urn:dk:some_domain:myPrivilege1C</Privilege>
            <Privilege>urn:dk:some_domain:myPrivilege1D</Privilege>
        </PrivilegeGroup>
    </bpp:PrivilegeList>

Is translated into two claims with JSON values:  
*(with extra spaces and line breaks for display purposes only)*

    {
        "cvr": "12345678",
        "p": [ "urn:dk:some_domain:myPrivilege1A", "urn:dk:some_domain:myPrivilege1B" ]
    }

and

    {
        "se": "27384223",
        "p": [ "urn:dk:some_domain:myPrivilege1C", "urn:dk:some_domain:myPrivilege1D" ]
    }


## Model 3
Model 3 is an extension to [Model 2](#model-2).

The `Constraint` element(s) are translated into the property `c` with the constraint(s) as a list of key value pairs.

DK privilege base64-decoded sample:  
*(with extra spaces and line breaks for display purposes only)*

    <?xml version="1.0" encoding="UTF-8"?>
    <bpp:PrivilegeList xmlns:bpp="http://digst.dk/oiosaml/basic_privilege_profile" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" >
        <PrivilegeGroup Scope="urn:dk:gov:saml:cvrNumberIdentifier:12345678">
            <Constraint Name="urn:dk:kombit:KLE">25.*</Constraint>
            <Constraint Name="urn:dk:kombit:sensitivity">3</Constraint>
            <Privilege>urn:dk:kombit:system_xyz:view_case</Privilege>
        </PrivilegeGroup>
    </bpp:PrivilegeList>

Is translated into one claims with JSON values:  
*(with extra spaces and line breaks for display purposes only)*

    {
        "cvr": "12345678",
        "c": [ { "urn:dk:kombit:KLE": "25.*" }, { "urn:dk:kombit:sensitivity": "3" } ]
        "p": [ "urn:dk:kombit:system_xyz:view_case" ]
    }

## Using JSON privilege claim in an application
The [application registration](connections.md#application-registration) application receives the privilege claim with the privilege serialized as a JSON string.  
The following C# code example show how to deserialize the JSON claim to an object in ASP.NET Core application using `Newtonsoft.Json`.

Create privilege group class

    public class DkPrivilegeGroup
    {
        [JsonProperty(PropertyName = "cvr")]
        public string CvrNumber { get; set; }

        [JsonProperty(PropertyName = "pu")]
        public string ProductionUnit { get; set; }

        [JsonProperty(PropertyName = "se")]
        public string SeNumber { get; set; }

        [JsonProperty(PropertyName = "cpr")]
        public string CprNumber { get; set; }

        [JsonProperty(PropertyName = "c")]
        public Dictionary<string, string> Constraint { get; set; }

        [JsonProperty(PropertyName = "p")]
        public List<string> Privilege { get; set; }
    }

and deserialize the claim in e.g., a controller

    var privileges = User.Claims.Where(c => c.Type == "privilege")
        .Select(c => JsonConvert.DeserializeObject<DkPrivilegeGroup>(c.Value)).ToList();
    foreach(var privilege in privileges)
    {
        // TODO handle access based on the privilege
    }