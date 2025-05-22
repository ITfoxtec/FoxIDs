using DeepL;
using FoxIDs.Models;
using ITfoxtec.Identity;
using System.Globalization;

namespace FoxIDs.ResourceTranslateTool.Logic
{
    public class DeepLTranslateLogic
    {
        private readonly ResourceLogic resourceLogic;
        private readonly Translator translator;

        public DeepLTranslateLogic(ResourceLogic resourceLogic, Translator translator)
        {
            this.resourceLogic = resourceLogic;
            this.translator = translator;
        }

        public async Task TranslateAllAsync()
        {
            var languageCodes = GetLanguageCodes();
            resourceLogic.UpdateSupportedCultures(languageCodes);

            foreach (var resource in resourceLogic.ResourceEnvelope.Resources)
            {
                try
                {
                    var text = resource.Items.Where(i => i.Culture == LanguageCode.English).Select(i => i.Value).Single();
                    Console.Write($"Translating resource [{resource.Id}]: '{text}'");

                    var cultures = resource.Items.Select(i => i.Culture);
                    var resourceLanguageCodes = languageCodes.Where(c => !cultures.Contains(c.Substring(0, 2))).ToList();

                    if (resourceLanguageCodes.Count() > 0)
                    {
                        Console.Write(", language codes: ");
                    }

                    foreach (var languageCode in resourceLanguageCodes)
                    {
                        Console.Write($", {languageCode}");
                        resource.Items.Add(new ResourceCultureItem
                        {
                            EditLevel = ResourceEditLevels.MachineDeepL,
                            Culture = new CultureInfo(languageCode).TwoLetterISOLanguageName,
                            Value = (await translator.TranslateTextAsync(text, LanguageCode.English, languageCode)).Text,
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

        private IEnumerable<string> GetLanguageCodes()
        {
            yield return LanguageCode.Bulgarian;
            yield return LanguageCode.Czech;
            yield return LanguageCode.Danish;
            yield return LanguageCode.Dutch;
            yield return LanguageCode.Estonian;
            yield return LanguageCode.Finnish;
            yield return LanguageCode.French;
            yield return LanguageCode.German;
            yield return LanguageCode.Greek;
            yield return LanguageCode.Italian;
            yield return LanguageCode.Latvian;
            yield return LanguageCode.Lithuanian;
            yield return LanguageCode.Norwegian;
            yield return LanguageCode.Polish;
            yield return LanguageCode.PortugueseEuropean;
            yield return LanguageCode.Romanian;
            yield return LanguageCode.Slovak;
            yield return LanguageCode.Slovenian;
            yield return LanguageCode.Spanish;
            yield return LanguageCode.Swedish;
            yield return LanguageCode.Turkish;
            yield return LanguageCode.Ukrainian;
        }
    }
}
