using FoxIDs.Logic;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace FoxIDs.Infrastructure.Localization
{
    public class FoxIDsStringLocalizer : IStringLocalizer
    {
        private readonly LocalizationLogic localizationLogic;
        private readonly IHttpContextAccessor httpContextAccessor;

        public FoxIDsStringLocalizer(LocalizationLogic localizationLogic, IHttpContextAccessor httpContextAccessor)
        {
            this.localizationLogic = localizationLogic;
            this.httpContextAccessor = httpContextAccessor;
        }

        public LocalizedString this[string name] => GetString(name);

        public LocalizedString this[string name, params object[] arguments] => GetString(name, arguments);

        private LocalizedString GetString(string name, params object[] arguments)
        {
            var culture = httpContextAccessor.HttpContext.GetCulture();

            var value = localizationLogic.GetValue(name, culture.Name);
#if DEBUG
            if(value.IsNullOrEmpty())
            {
                localizationLogic.SaveResource(name);
            }
#endif
            value = value ?? name;

            if (arguments?.Length > 0)
            {
                value = string.Format(value, arguments.ToArray());
            }

            return new LocalizedString(name, value);
        }

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        {
            throw new NotSupportedException();
        }

        public IStringLocalizer WithCulture(CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
