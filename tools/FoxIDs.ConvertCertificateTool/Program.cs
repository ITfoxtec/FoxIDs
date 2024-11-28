using System.Security.Cryptography.X509Certificates;

namespace FoxIDs.ConvertCertificateTool
{
    class Program
    {
        static void Main(string[] args)
        {
            var certificateFileName = "oces3_-test-_systemcertifikat";
            var password = "c5,PnmF8;m4I";

            var certificate = X509CertificateLoader.LoadPkcs12FromFile($"{certificateFileName}.p12", password, X509KeyStorageFlags.Exportable);
            File.WriteAllBytes($"{certificateFileName}.pfx", certificate.Export(X509ContentType.Pfx, password));

            Console.WriteLine("Done!");
        }
    }
}