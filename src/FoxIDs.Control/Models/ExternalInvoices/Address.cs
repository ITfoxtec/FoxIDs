﻿using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.ExternalInvoices
{
    public abstract class Address
    {
        /// <summary>
        /// Company name or name.
        /// </summary>
        [Required]
        [MaxLength(Constants.Models.Address.NameLength)]
        public string Name { get; set; }

        [MaxLength(Constants.Models.Address.VatNumberLength)]
        public string VatNumber { get; set; }

        [MaxLength(Constants.Models.Address.AddressLine1Length)]
        public string AddressLine1 { get; set; }

        [MaxLength(Constants.Models.Address.AddressLine2Length)]
        public string AddressLine2 { get; set; }

        [MaxLength(Constants.Models.Address.PostalCodeLength)]
        public string PostalCode { get; set; }

        [MaxLength(Constants.Models.Address.CityLength)]
        public string City { get; set; }

        [MaxLength(Constants.Models.Address.StateRegionLength)]
        public string StateRegion { get; set; }

        [MaxLength(Constants.Models.Address.CountryLength)]
        public string Country { get; set; }
    }
}