using FoxIDs.ResourceTranslateTool.Infrastructure;
using FoxIDs.ResourceTranslateTool.Logic;
using Microsoft.Extensions.DependencyInjection;

try
{
    var serviceProvider = new StartupConfigure().ConfigureServices();

    Console.WriteLine("Starts translating all resource texts...");

    var resourceLogic = serviceProvider.GetService<ResourceLogic>();
    await resourceLogic.LoadResourcesAsync();

    Console.WriteLine("DeepL");
    await serviceProvider.GetService<DeepLTranslateLogic>().TranslateAllAsync();
    await resourceLogic.SaveResourcesAsync();

    Console.WriteLine(string.Empty);
    Console.WriteLine("Google");
    await serviceProvider.GetService<GoogleTranslateLogic>().TranslateAllAsync();
    await resourceLogic.SaveResourcesAsync();

}
catch (Exception ex)
{
    Console.WriteLine(string.Empty);
    Console.WriteLine(string.Empty);
    Console.WriteLine($"Error: {ex}");
}

Console.WriteLine(string.Empty);
Console.WriteLine("The translation is complete, click any key to end...");
Console.ReadKey();