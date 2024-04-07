# FoxIDs development / code contribution 

You can run FoxIDs locally in Visual Studio on your development machine. 

**Clone the [FoxIDs GitHub repository](https://github.com/ITfoxtec/FoxIDs) and possibly do pull requests.**
 
Solution description:

- **FoxIDs** is the FoxIDs Identity Service ASP.NET Core application  
  Local endpoint `https://localhost:44330`
- **FoxIDs.Control** is the FoxIDs Control ASP.NET Core API and host of the FoxIDs Control Client Blazor WebAssembly  
  Local endpoint `https://localhost:44331`  
  Local API endpoint `https://localhost:44331/api/`  
  API Swagger (OpenApi) endpoint `https://localhost:44331/api/swagger/v1/swagger.json`
- **FoxIDs.ControlClient** is the FoxIDs Control Client Blazor WebAssembly  
- **FoxIDs.ControlShared** is a library shared between the FoxIDs Control API backend and FoxIDs Control Client Blazor WebAssembly frontend
- **FoxIDs.Shared** is a library shared between the FoxIDs ASP.NET Core application and FoxIDs Control ASP.NET Core API
- **FoxIDs.SharedBase** is a library shared by all


Open the FoxIDs solution in Visual Studio or in your preferred developer tool.

The solution is default configured to rune locally and to use a file store for both data and cache. The files are by default saved in the `data` folder in the root of the solution folder.

The default configuration file `appsettings.json` in the FoxIDs project: 

    {
      ...
      "Settings": {   
        "FoxIDsEndpoint": "https://localhost:44330",
        "Options": {
          "Log": "Stdout",
          "DataStorage": "File",
          "KeyStorage": "None",
          "Cache": "File",
          "DataCache": "None"
        }
      }
    }


The default configuration file `appsettings.json` in the FoxIDs.Control project: 

    {
      ...
      "Settings": {   
        "FoxIDsEndpoint": "https://localhost:44330",
        "FoxIDsControlEndpoint": "https://localhost:44331",
        "Options": {
          "Log": "Stdout",
          "DataStorage": "File",
          "KeyStorage": "None",
          "Cache": "File",
          "DataCache": "None"
        }
      }
    }


Hit run! The FoxIDs Control site should be lunched in a browser.

Login with the default admin user `admin@foxids.com` with password `FirstAccess!`

After successfully login you have access to the master tenant. You should then create a dev tenant where you can add applications (application registration), APIs (application registration), user login (authentication method) and external trust (authentication method).  
After having your dev tenant created you can follow the [get started guide](get-started.md#2-first-login).

## API client proxy

It is possible to integrate with the FoxIDs Control API in different ways, it is just a plain API exposing an interface description with Swagger (OpenApi). 

It is e.g., possible to generate client code with NSwag:
- Generate code with Visual Studio extension https://github.com/dmitry-pavlov/api-client-generation-tools.
- Generate code with NSwagStudio https://github.com/RicoSuter/NSwag/wiki/NSwagStudio. Microsoft description https://docs.microsoft.com/en-us/aspnet/core/tutorials/getting-started-with-nswag?view=aspnetcore-2.2&tabs=visual-studio#generate-code-with-nswagstudio.
- Automatically generating API clients on build with NSwag https://blog.sanderaernouts.com/autogenerate-api-client-with-nswag

> You can find a code sample in [FoxIDs.SampleSeedTool](https://github.com/ITfoxtec/FoxIDs.Samples/tree/master/tools/FoxIDs.SampleSeedTool) which automatically generating an API clients on build. When the [GenerateCode](https://github.com/ITfoxtec/FoxIDs.Samples/blob/master/tools/FoxIDs.SampleSeedTool/FoxIDs.SampleSeedTool.csproj#L9C17-L9C22) is true in the project file.

