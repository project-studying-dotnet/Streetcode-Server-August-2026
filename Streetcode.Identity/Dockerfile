FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["Streetcode.Identity/src/Streetcode.Identity.Domain/Streetcode.Identity.Domain.csproj", "Streetcode.Identity/src/Streetcode.Identity.Domain/"]
COPY ["Streetcode.Identity/src/Streetcode.Identity.Application/Streetcode.Identity.Application.csproj", "Streetcode.Identity/src/Streetcode.Identity.Application/"]
COPY ["Streetcode.Identity/src/Streetcode.Identity.Infrastructure/Streetcode.Identity.Infrastructure.csproj", "Streetcode.Identity/src/Streetcode.Identity.Infrastructure/"]
COPY ["Streetcode.Identity/src/Streetcode.Identity.WebApi/Streetcode.Identity.WebApi.csproj", "Streetcode.Identity/src/Streetcode.Identity.WebApi/"]

RUN dotnet restore \
    "Streetcode.Identity/src/Streetcode.Identity.WebApi/Streetcode.Identity.WebApi.csproj"

COPY ["Streetcode.Identity/src/", "Streetcode.Identity/src/"]

WORKDIR "/src/Streetcode.Identity/src/Streetcode.Identity.WebApi"

RUN dotnet publish \
    "Streetcode.Identity.WebApi.csproj" \
    --configuration "$BUILD_CONFIGURATION" \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR /app

EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080

COPY --from=build /app/publish .

USER $APP_UID

ENTRYPOINT ["dotnet", "Streetcode.Identity.WebApi.dll"]
