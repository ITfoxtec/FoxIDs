using DeepL;
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

        public bool AddDefaultEnResource()
        {
            var updated = false;
            foreach (var nameItem in ResourceEnvelope.Names)
            {
                if (!ResourceEnvelope.Resources.Where(r => r.Id == nameItem.Id).Any())
                {
                    ResourceEnvelope.Resources.Add(new ResourceItem
                    {
                        Id = nameItem.Id,
                        Items = [new ResourceCultureItem {  Culture = LanguageCode.English, EditLevel = ResourceEditLevels.Human, Value = nameItem.Name }]
                    });
                    updated = true;
                }
            }
            return updated;
        }
    }
}
