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
            var startTime = DateTime.UtcNow;
            try
            {
                var serviceProvider = new StartupConfigure().ConfigureServices();

                Console.WriteLine("Select seed action:");
                Console.WriteLine("U: Upload users to an environment");
                Console.WriteLine("D: Delete all users in an environment");

                var key = Console.ReadKey();
                Console.WriteLine(string.Empty);
                Console.WriteLine(string.Empty);

                switch (char.ToLower(key.KeyChar))
                {
                    case 'u':
                        await serviceProvider.GetService<UserSeedLogic>().SeedAsync();
                        break;
                    case 'd':
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

            var endTime = DateTime.UtcNow;
            Console.WriteLine(string.Empty);
            Console.WriteLine($"Start time (UTC): {startTime:O}");
            Console.WriteLine($"End time (UTC):   {endTime:O}");
            Console.WriteLine($"Time used:        {endTime - startTime}");

            Console.WriteLine(string.Empty);
            Console.WriteLine("Click any key to end...");
            Console.ReadKey();
        }
    }
}
