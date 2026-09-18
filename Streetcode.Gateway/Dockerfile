FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Streetcode.Gateway/Streetcode.Gateway.csproj Streetcode.Gateway/
RUN dotnet restore Streetcode.Gateway/Streetcode.Gateway.csproj
COPY Streetcode.Gateway/ Streetcode.Gateway/
RUN dotnet publish Streetcode.Gateway/Streetcode.Gateway.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "Streetcode.Gateway.dll"]
