﻿using ITfoxtec.Identity;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public class NewUpPartyNemLoginViewModel : IValidatableObject
    {
        [Required]
        [MaxLength(Constants.Models.Party.DisplayNameLength)]
        [RegularExpression(Constants.Models.Party.DisplayNameRegExPattern)]
        [Display(Name = "Name")]
        public string DisplayName { get; set; }

        [Display(Name = "NemLog-in environment")]
        public NemLoginEnvironments NemLoginEnvironment { get; set; } = NemLoginEnvironments.Test;

        [Display(Name = "NemLog-in service type")]
        public NemLoginServiceTypes NemLoginServiceType { get; set; } = NemLoginServiceTypes.PublicSector;

        [Display(Name = "NemLog-in claims")]
        public IEnumerable<string> Claims
        {
            get 
            {
                yield return "https://data.gov.dk/model/core/specVersion";
                yield return "https://data.gov.dk/concept/core/nsis/loa";
                yield return "https://data.gov.dk/model/core/eid/fullName";
                yield return "https://data.gov.dk/model/core/eid/firstName";
                yield return "https://data.gov.dk/model/core/eid/lastName";
                yield return "https://data.gov.dk/model/core/eid/email";
                if (NemLoginServiceType == NemLoginServiceTypes.PublicSector)
                {
                    yield return "https://data.gov.dk/model/core/eid/cprNumber";
                }
                yield return "https://data.gov.dk/model/core/eid/cprUuid";
                yield return "https://data.gov.dk/model/core/eid/person/pid";
                yield return "https://data.gov.dk/model/core/eid/age";
                yield return "https://data.gov.dk/model/core/eid/dateOfBirth";
                yield return "https://data.gov.dk/model/core/eid/professional/cvr";
                yield return "https://data.gov.dk/model/core/eid/professional/orgName";
                yield return "https://data.gov.dk/model/core/eid/professional/uuid/persistent";
                yield return "https://data.gov.dk/model/core/eid/professional/rid";
                yield return "https://data.gov.dk/model/core/eid/professional/productionUnit";
                yield return "https://data.gov.dk/model/core/eid/professional/seNumber";
                yield return "https://data.gov.dk/model/core/eid/privilegesIntermediate";
            }
        }

        public List<string> DisabledClaims { get; set; } = new List<string>
        {
            "https://data.gov.dk/model/core/specVersion",
            "https://data.gov.dk/concept/core/nsis/loa",
        };
        public List<string> SelectedClaims { get; set; } = new List<string>
        {
            "https://data.gov.dk/model/core/specVersion",
            "https://data.gov.dk/concept/core/nsis/loa",
            "https://data.gov.dk/model/core/eid/firstName",
            "https://data.gov.dk/model/core/eid/lastName",
            "https://data.gov.dk/model/core/eid/email",
            "https://data.gov.dk/model/core/eid/cprNumber",
            "https://data.gov.dk/model/core/eid/cprUuid",
            "https://data.gov.dk/model/core/eid/professional/cvr",
            "https://data.gov.dk/model/core/eid/professional/orgName",
            "https://data.gov.dk/model/core/eid/professional/uuid/persistent",
            "https://data.gov.dk/model/core/eid/privilegesIntermediate",
        };

        [MaxLength(Constants.Models.Claim.LimitedValueLength)]
        [Display(Name = "Company")]
        public string Company { get; set; }

        //[MaxLength(Constants.Models.Claim.LimitedValueLength)]
        //[Display(Name = "Given name (optional)")]
        //public string GivenName { get; set; }

        //[MaxLength(Constants.Models.Claim.LimitedValueLength)]
        //[Display(Name = "Surname (optional)")]
        //public string Surname { get; set; }

        [MaxLength(Constants.Models.Claim.LimitedValueLength)]
        [EmailAddress]
        [Display(Name = "Email address")]
        public string EmailAddress { get; set; }

        //[MaxLength(Constants.Models.Claim.LimitedValueLength)]
        //[Display(Name = "Telephone number (optional)")]
        //public string TelephoneNumber { get; set; }

        [Display(Name = "Metadata URL")]
        public string Metadata { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if (NemLoginEnvironment == NemLoginEnvironments.Production)
            {
                if(Company.IsNullOrWhiteSpace())
                {
                    results.Add(new ValidationResult($"The company is required.", [nameof(Company)]));
                }
                if (EmailAddress.IsNullOrWhiteSpace())
                {
                    results.Add(new ValidationResult($"The email is required.", [nameof(EmailAddress)]));
                }
            }
            return results;
        }
    }
}
