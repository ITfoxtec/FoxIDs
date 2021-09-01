# Convert certificate file

This tool can convert a certificate from a `.P12` file to a `.PFX` file.

> You can upload a primary or secondary certificate to FoxIDs. FoxIDs required the certificate to be a `.PFX` file.

Sample code:

    var certificateFileName = "some-service-provider";
    var password = "Abcd#1234";

    var certificate = new X509Certificate2($"{certificateFileName}.p12", password, X509KeyStorageFlags.Exportable | X509KeyStorageFlags.EphemeralKeySet);
    File.WriteAllBytes($"{certificateFileName}.pfx", certificate.Export(X509ContentType.Pfx, password));