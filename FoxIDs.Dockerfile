FROM mcr.microsoft.com/dotnet/sdk:8.0 as build
RUN mkdir /build
COPY . /build
WORKDIR /build
RUN rm -rf ./src/FoxIDs/web.config
RUN rm -rf ./src/FoxIDs/web.Release.config
RUN dotnet publish ./src/FoxIDs/FoxIDs.csproj -c Debug -o FoxIDs
FROM mcr.microsoft.com/dotnet/aspnet:8.0
EXPOSE 80
COPY --from=build /build/FoxIDs /app
WORKDIR /app
#EXPOSE 80
#EXPOSE 443
#ENV ASPNETCORE_ENVIRONMENT=Development
#ENV ASPNETCORE_URLS=http://+:80
ENTRYPOINT ["/app/FoxIDs"]
