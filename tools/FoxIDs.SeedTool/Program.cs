using System;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using FoxIDs.SeedTool.Infrastructure;
using FoxIDs.SeedTool.SeedLogic;

namespace FoxIDs.SeedTool
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

                Console.WriteLine("Select seed action");
                Console.WriteLine("R: Upload risk passwords");
                Console.WriteLine("I: Delete all risk passwords in one DB call (recommended, only supported by MongoDB)");
                Console.WriteLine("S: Delete all risk passwords in many DB calls");

                var key = Console.ReadKey();
                Console.WriteLine(string.Empty);
                Console.WriteLine(string.Empty);

                switch (char.ToLower(key.KeyChar))
                {
                    case 'r':
                        await serviceProvider.GetService<RiskPasswordSeedLogic>().SeedAsync();
                        break;
                    case 'i':
                        await serviceProvider.GetService<RiskPasswordSeedLogic>().DeleteAllInPartitionAsync();
                        break;
                    case 's':
                        await serviceProvider.GetService<RiskPasswordSeedLogic>().DeleteAllAsync();
                        break;

                    default:
                        Console.WriteLine("Canceled");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex}");
            }

            Console.WriteLine(string.Empty);
            Console.WriteLine("Click any key to end...");
            Console.ReadKey();
        }
    }
}
