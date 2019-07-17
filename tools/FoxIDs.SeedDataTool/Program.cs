using System;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using FoxIDs.SeedDataTool.Infrastructure;
using FoxIDs.SeedDataTool.Logic;
using FoxIDs.SeedDataTool.SeedLogic;

namespace FoxIDs.SeedDataTool
{
    class Program
    {
        static void Main(string[] args)
        {
            MainAsync(args).GetAwaiter().GetResult();
        }

        static async Task MainAsync(string[] args)
        {
            try
            {
                var serviceProvider = new StartupConfigure().ConfigureServices();

                var accessToken = await serviceProvider.GetService<AccessLogic>().GetAccessTokenAsync();

                Console.WriteLine("Select seed action");
                Console.WriteLine($"M: {nameof(MasterTenantDocumentsSeedLogic)}");
                Console.WriteLine($"P: {nameof(PasswordRiskListSeedLogic)}");

                var key = Console.ReadKey();
                Console.WriteLine(string.Empty);


                switch (char.ToLower(key.KeyChar))
                {
                    case 'm':
                        Console.WriteLine(nameof(MasterTenantDocumentsSeedLogic));
                        await serviceProvider.GetService<MasterTenantDocumentsSeedLogic>().SeedAsync();
                        break;

                    case 'p':
                        Console.WriteLine(nameof(PasswordRiskListSeedLogic));
                        await serviceProvider.GetService<PasswordRiskListSeedLogic>().SeedAsync(accessToken);
                        break;

                    default:
                        Console.WriteLine("Canceled");
                        break;
                }

                Console.WriteLine("Done!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.ToString()}");
            }
        }
    }
}
