# Development

You can rune FoxIDs locally e.g., in Visual Studio on your development machine. 

If you like, you can clone the [FoxIDs GitHub repository](https://github.com/ITfoxtec/FoxIDs) and possible do pull requests.
 
Solution description:

- **FoxIDs** is the FoxIDs ASP.NET application  
  Local endpoint `https://localhost:44330`
- **FoxIDs.Control** is the FoxIDs Control ASP.NET API and host of the FoxIDs Control Client Blazor WebAssembly  
  Local endpoint `https://localhost:44331`  
  Local API endpoint `https://localhost:44331/api/` and API Swagger (OpenApi) endpoint `https://localhost:44331/api/swagger/v1/swagger.json`
- **FoxIDs.ControlClient** is the FoxIDs Control Client Blazor WebAssembly  
- **FoxIDs.ControlShared** is a library shared between the FoxIDs Control API backend and FoxIDs Control Client Blazor WebAssembly
- **FoxIDs.Shared** is a library shared between the FoxIDs ASP.NET application and FoxIDs Control ASP.NET API
- **FoxIDs.SharedBase** is a library shared by all

## Azure resources

FoxIDs depends on Azure resources to run locally. They need to be created in Azure and configured.

### Key Vault

### Cosmos DB

### Redis cache

### Application Insights and Log Analytics workspace

## Run and debug 

After the Azure resources is in place you should be able to run and debug the solution. First time you hit run and access FoxIDs Control on `https://localhost:44331` Cosmos DB is pre seeded. 

> The default admin user is: `admin@foxids.com` with password: `FirstAccess!` (you are required to change the password on first login)  

## API client proxy

It is possible to integrate with the FoxIDs Control API in different ways, it is just a plain API exposing an interface description with Swagger (OpenApi). 

It is e.g., possible to generate client code with NSwag:
- Generate code with Visual Studio extension https://github.com/dmitry-pavlov/api-client-generation-tools.
- Generate code with NSwagStudio https://github.com/RicoSuter/NSwag/wiki/NSwagStudio. Microsoft description https://docs.microsoft.com/en-us/aspnet/core/tutorials/getting-started-with-nswag?view=aspnetcore-2.2&tabs=visual-studio#generate-code-with-nswagstudio.
- Automatically generating API clients on build with NSwag https://blog.sanderaernouts.com/autogenerate-api-client-with-nswag

