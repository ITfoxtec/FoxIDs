FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER app
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/FoxIDs/FoxIDs.csproj", "FoxIDs/"]
COPY ["src/FoxIDs.Shared/FoxIDs.Shared.csproj", "FoxIDs.Shared/"]
COPY ["src/FoxIDs.SharedBase/FoxIDs.SharedBase.csproj", "FoxIDs.SharedBase/"]
RUN dotnet restore "FoxIDs/FoxIDs.csproj"
COPY . .
WORKDIR "/src/src/FoxIDs"
RUN dotnet build "FoxIDs.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "FoxIDs.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FoxIDs.dll"]