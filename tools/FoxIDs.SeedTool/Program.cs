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

                Console.WriteLine("Select seed action:");
                Console.WriteLine("U: Upload users in an environment");
                Console.WriteLine("E: Delete all users in an environment with one DB call (recommended, only supported by MongoDB)");
                Console.WriteLine("R: Delete all users in an environment with many DB calls");

                var key = Console.ReadKey();
                Console.WriteLine(string.Empty);
                Console.WriteLine(string.Empty);

                switch (char.ToLower(key.KeyChar))
                {
                    case 'u':
                        await serviceProvider.GetService<UserSeedLogic>().SeedAsync();
                        break;
                    case 'e':
                        await serviceProvider.GetService<UserSeedLogic>().DeleteAllInPartitionAsync();
                        break;
                    case 'r':
                        await serviceProvider.GetService<UserSeedLogic>().DeleteAllAsync();
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
