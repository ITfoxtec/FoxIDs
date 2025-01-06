using FoxIDs.Models.Logic;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;

namespace FoxIDs.Logic
{
    public class CountryCodesLogic : LogicBase
    {
        private List<CountryCode> countryCodes;
        private bool isInitiated = false;
        private readonly IHttpContextAccessor httpContextAccessor;

        public CountryCodesLogic(IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.httpContextAccessor = httpContextAccessor;
        }

        public string GetCountryCodeStringByCulture()
        {
            LoadCountryCodes();
            var countryCode = GetCountryCode(httpContextAccessor.HttpContext.GetUiCulture());
            if (countryCode == null)
            {
                countryCode = GetCountryCode(httpContextAccessor.HttpContext.GetCulture());
            }
            return countryCode != null ? $"+{countryCode.PhoneCode}" : null;
        }

        private CountryCode GetCountryCode(CultureInfo culture)
        {
            if (culture == null)
            {
                return null;
            }

            var countryCode = countryCodes.Where(c => c.LanguageCodes.Equals(culture.Name, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
            if (countryCode == null && !string.IsNullOrEmpty(culture.Parent?.Name))
            {
                countryCode = countryCodes.Where(c => c.LanguageCodes.Equals(culture.Parent.Name, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
            }

            return countryCode;
        }

        public string ReturnPhoneNotCountryCode(string phone)
        {
            if (phone.IsNullOrWhiteSpace())
            {
                return null;
            }

            LoadCountryCodes();
            var testPhone = phone.Trim().TrimStart('+');
            if (countryCodes.Where(c => c.PhoneCode.Equals(testPhone, StringComparison.OrdinalIgnoreCase)).Any())
            {
                return null;
            }
            else
            {
                return phone;
            }
        }

        private void LoadCountryCodes()
        {
            if (!isInitiated)
            {
                using (var reader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream(CountryCodesName)))
                {
                    countryCodes = reader.ReadToEnd().ToObject<List<CountryCode>>();
                }
                isInitiated = true;
            }
        }

        private string CountryCodesName => $"{typeof(CountryCodes).FullName}.json";
    }
}
