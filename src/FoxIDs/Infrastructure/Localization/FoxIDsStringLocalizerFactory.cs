using Microsoft.Extensions.Localization;
using System;

namespace FoxIDs.Infrastructure.Localization
{
    public class FoxIDsStringLocalizerFactory : IStringLocalizerFactory
    {
        private readonly IStringLocalizer stringLocalizer;

        public FoxIDsStringLocalizerFactory(IStringLocalizer stringLocalizer)
        {
            this.stringLocalizer = stringLocalizer;
        }

        public IStringLocalizer Create(Type resourceSource)
        {
            return stringLocalizer;
        }

        public IStringLocalizer Create(string baseName, string location)
        {
            return stringLocalizer;
        }
    }
}
