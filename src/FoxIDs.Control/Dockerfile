FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/FoxIDs.Control/FoxIDs.Control.csproj", "FoxIDs.Control/"]
COPY ["src/FoxIDs.ControlClient/FoxIDs.ControlClient.csproj", "FoxIDs.ControlClient/"]
COPY ["src/FoxIDs.ControlShared/FoxIDs.ControlShared.csproj", "FoxIDs.ControlShared/"]
COPY ["src/FoxIDs.SharedBase/FoxIDs.SharedBase.csproj", "FoxIDs.SharedBase/"]
COPY ["src/FoxIDs.Shared/FoxIDs.Shared.csproj", "FoxIDs.Shared/"]
RUN dotnet restore "FoxIDs.Control/FoxIDs.Control.csproj"
COPY . .
WORKDIR "/src/src/FoxIDs.Control"
RUN dotnet build "FoxIDs.Control.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "FoxIDs.Control.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FoxIDs.Control.dll"]