FROM mcr.microsoft.com/dotnet/sdk:8.0 as build
RUN mkdir /build
COPY . /build
WORKDIR /build
RUN rm -rf ./src/FoxIDs.Control/web.config
RUN rm -rf ./src/FoxIDs.Control/web.Release.config
RUN dotnet publish ./src/FoxIDs.Control/FoxIDs.Control.csproj -c Debug -o FoxIDs.Control
FROM mcr.microsoft.com/dotnet/aspnet:8.0
COPY --from=build /build/FoxIDs.Control /app
WORKDIR /app
ENV ASPNETCORE_ENVIRONMENT=Development
ENV ASPNETCORE_URLS=https://+:44331
ENTRYPOINT ["/app/FoxIDs.Control"]
