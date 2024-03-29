FROM mcr.microsoft.com/dotnet/sdk:8.0 as build
RUN mkdir /build
COPY . /build
WORKDIR /build
RUN rm -rf ./FoxIDs.Control/web.config
RUN rm -rf ./FoxIDs.Control/web.Release.config
RUN dotnet publish ./FoxIDs.Control/FoxIDs.Control.csproj -c Release -o FoxIDs.Control
FROM mcr.microsoft.com/dotnet/runtime:8.0
COPY --from=build /build/FoxIDs.Control /app
WORKDIR /app
ENTRYPOINT ["/app/FoxIDs.Control"]
