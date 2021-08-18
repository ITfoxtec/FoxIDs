using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;

namespace FoxIDs.ConvertCertificateTool
{
    class Program
    {
        static void Main(string[] args)
        {
            var certificateFileName = "serviceprovider";
            var password = "Test1234";

            var certificate = new X509Certificate2($"{certificateFileName}.p12", password, X509KeyStorageFlags.Exportable | X509KeyStorageFlags.EphemeralKeySet);
            File.WriteAllBytes($"{certificateFileName}.pfx", certificate.Export(X509ContentType.Pfx, password));

            Console.WriteLine("Done!");
        }
    }
}
