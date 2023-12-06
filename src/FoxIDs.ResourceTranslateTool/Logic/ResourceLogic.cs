using FoxIDs.Models;
using FoxIDs.ResourceTranslateTool.Models;
using ITfoxtec.Identity;
using System.Globalization;

namespace FoxIDs.ResourceTranslateTool.Logic
{
    public class ResourceLogic
    {
        private readonly TranslateSettings translateSettings;

        public ResourceLogic(TranslateSettings translateSettings)
        {
            this.translateSettings = translateSettings;
        }

        public ResourceEnvelope ResourceEnvelope { get; set; }

        public async Task LoadResourcesAsync()
        {
            var json = await File.ReadAllTextAsync(translateSettings.EmbeddedResourceJsonPath);
            await json.ValidateObjectAsync();
            ResourceEnvelope = json.ToObject<ResourceEnvelope>();
        }

        public async Task SaveResourcesAsync()
        {
            var json = ResourceEnvelope.ToJsonIndented();
            await json.ValidateObjectAsync();
            await File.WriteAllTextAsync(translateSettings.EmbeddedResourceJsonPath, json);
        }

        public void UpdateSupportedCultures(IEnumerable<string> languageCodes)
        {
            var isoLanguageCodes = languageCodes.Select(l => new CultureInfo(l).TwoLetterISOLanguageName);
            ResourceEnvelope.SupportedCultures.ConcatOnce(isoLanguageCodes);
            ResourceEnvelope.SupportedCultures = ResourceEnvelope.SupportedCultures.OrderBy(c => c).ToList();
        }
    }
}
