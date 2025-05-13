using DeepL;
using FoxIDs.Models;
using FoxIDs.ResourceTranslateTool.Models;
using Google.Api.Gax.ResourceNames;
using Google.Cloud.Translate.V3;
using ITfoxtec.Identity;
using Pipelines.Sockets.Unofficial.Arenas;
using System.Globalization;

namespace FoxIDs.ResourceTranslateTool.Logic
{
    public class GoogleTranslateLogic
    {
        private readonly ResourceLogic resourceLogic;
        private readonly TranslateSettings translateSettings;

        public GoogleTranslateLogic(ResourceLogic resourceLogic, TranslateSettings translateSettings)
        {
            this.resourceLogic = resourceLogic;
            this.translateSettings = translateSettings;
        }

        public async Task TranslateAllAsync()
        {
            var languageCodes = GetLanguageCodes();
            resourceLogic.UpdateSupportedCultures(languageCodes);

            foreach (var resource in resourceLogic.ResourceEnvelope.Resources)
            {
                try
                {
                    var translationServiceClient = await TranslationServiceClient.CreateAsync();

                    var text = resource.Items.Where(i => i.Culture == LanguageCode.English).Select(i => i.Value).Single();
                    Console.Write($"Translating resource [{resource.Id}]: '{text}'");

                    var cultures = resource.Items.Select(i => i.Culture);
                    var resourceLanguageCodes = languageCodes.Where(c => !cultures.Contains(c)).ToList();

                    if (resourceLanguageCodes.Count() > 0)
                    {
                        Console.Write(", language codes: ");
                    }

                    foreach (var languageCode in resourceLanguageCodes)
                    {
                        Console.Write($", {languageCode}");
                        resource.Items.Add(new ResourceCultureItem
                        {
                            EditLevel = ResourceEditLevels.MachineGoogle,
                            Culture = new CultureInfo(languageCode).TwoLetterISOLanguageName,
                            Value = await TranslateTextAsync(translationServiceClient, text, languageCode)
                        });
                    }

                    resource.Items = resource.Items.OrderBy(i => i.Culture).ToList();
                    Console.WriteLine($" - done.");
                    Console.WriteLine(string.Empty);

                    await resourceLogic.SaveResourcesAsync();
                }
                catch (Exception ex)
                {
                    throw new Exception($"Resource '{resource.ToJson()}' translation error.", ex);
                }
            }
        }

        public async Task TranslateAllByInputAsync()
        {
            var languageCodes = GetLanguageCodes();
            resourceLogic.UpdateSupportedCultures(languageCodes);

            Console.WriteLine($"Translate by hand from: https://translate.google.com/");
            Console.WriteLine(string.Empty);

            foreach (var resource in resourceLogic.ResourceEnvelope.Resources)
            {
                var text = resource.Items.Where(i => i.Culture == LanguageCode.English).Select(i => i.Value).Single();
                Console.WriteLine($"Translating EN resource [{resource.Id}]: {text}");

                var cultures = resource.Items.Select(i => i.Culture);
                var resourceLanguageCodes = languageCodes.Where(c => !cultures.Contains(c)).ToList();
                
                foreach (var languageCode in resourceLanguageCodes)
                {
                    var culture = new CultureInfo(languageCode).TwoLetterISOLanguageName;
                    Console.WriteLine($"To language codes: {culture} - {LanguageName(culture)}");
                    Console.Write("Translated text: ");

                    resource.Items.Add(new ResourceCultureItem
                    {
                        EditLevel = ResourceEditLevels.MachineGoogle,
                        Culture = culture,
                        Value = Console.ReadLine()
                    });
                    Console.WriteLine(string.Empty);
                }

                resource.Items = resource.Items.OrderBy(i => i.Culture).ToList();
                Console.WriteLine($" - done.");
                Console.WriteLine(string.Empty);

                await resourceLogic.SaveResourcesAsync();
            }
        }

        private async Task<string> TranslateTextAsync(TranslationServiceClient client, string text, string languageCode)
        {
            var request = new TranslateTextRequest
            {
                Contents = { text },
                SourceLanguageCode = "en",
                TargetLanguageCode = languageCode,
                Parent = new ProjectName(translateSettings.GoogleProjectId).ToString() 
            };
            var response = await client.TranslateTextAsync(request);
            Translation translation = response.Translations[0];
            return translation.TranslatedText;
        }

        private IEnumerable<string> GetLanguageCodes()
        {
            yield return "ca"; // Catalan
            yield return "hr"; // Croatian
            yield return "is"; // Icelandic
        }

        private string LanguageName(string culture)
        {
            switch (culture)
            {
                case "ca":
                    return "Catalan";
                case "hr":
                    return "Croatian";
                case "is":
                    return "Icelandic";
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
