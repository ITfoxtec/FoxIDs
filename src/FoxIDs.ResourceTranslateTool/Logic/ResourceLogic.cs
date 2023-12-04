using FoxIDs.Models;
using FoxIDs.ResourceTranslateTool.Models;
using ITfoxtec.Identity;

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
            ResourceEnvelope = json.ToObject<ResourceEnvelope>();
        }

        public async Task SaveResourcesAsync()
        {
            await File.WriteAllTextAsync(translateSettings.EmbeddedResourceJsonPath, ResourceEnvelope.ToJsonIndented());
        }
    }
}
