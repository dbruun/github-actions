FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source
COPY src/DemoWeb/DemoWeb.csproj src/DemoWeb/
RUN dotnet restore src/DemoWeb/DemoWeb.csproj
COPY src/DemoWeb/ src/DemoWeb/
RUN dotnet publish src/DemoWeb/DemoWeb.csproj --configuration Release --no-restore --output /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "DemoWeb.dll"]
