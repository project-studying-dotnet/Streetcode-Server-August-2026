FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

ARG BUILD_CONFIGURATION=Release

WORKDIR /src

COPY ["Streetcode/Streetcode.ApiGateway/Streetcode.ApiGateway.csproj", "Streetcode/Streetcode.ApiGateway/"]

RUN dotnet restore "Streetcode/Streetcode.ApiGateway/Streetcode.ApiGateway.csproj"

COPY . .

WORKDIR "/src/Streetcode/Streetcode.ApiGateway"

RUN dotnet build "Streetcode.ApiGateway.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/build

FROM build AS publish

ARG BUILD_CONFIGURATION=Release

RUN dotnet publish "Streetcode.ApiGateway.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR /app

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "Streetcode.ApiGateway.dll"]