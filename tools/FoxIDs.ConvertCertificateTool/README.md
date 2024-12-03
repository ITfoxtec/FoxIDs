# Convert certificate file

This tool can convert a certificate from a `.P12` file to a `.PFX` file.

> You can upload a primary or secondary certificate to FoxIDs. Azure App Services and thereby FoxIDs support `.PFX` file certificates but not all `.P12` file certificates at this point.

Sample code:

    var certificateFileName = "some-service-provider";
    var password = "Abcd#1234";

    var certificate = X509CertificateLoader.LoadPkcs12FromFile($"{certificateFileName}.p12", password, X509KeyStorageFlags.Exportable);
    File.WriteAllBytes($"{certificateFileName}.pfx", certificate.Export(X509ContentType.Pfx, password));