using System.Security.Cryptography.X509Certificates;

namespace FoxIDs.ConvertCertificateTool
{
    class Program
    {
        static void Main(string[] args)
        {
            var certificateFileName = "oces3_-test-_systemcertifikat";
            var password = "c5,PnmF8;m4I";

            var certificate = new X509Certificate2($"{certificateFileName}.p12", password, X509KeyStorageFlags.Exportable | X509KeyStorageFlags.EphemeralKeySet);
            File.WriteAllBytes($"{certificateFileName}.pfx", certificate.Export(X509ContentType.Pfx, password));

            Console.WriteLine("Done!");
        }
    }
}