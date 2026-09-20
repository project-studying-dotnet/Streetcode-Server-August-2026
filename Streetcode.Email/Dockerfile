FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base

WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080

EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

ARG BUILD_CONFIGURATION=Release

WORKDIR /src

COPY ["src/Streetcode.Email.Domain/Streetcode.Email.Domain.csproj", "src/Streetcode.Email.Domain/"]
COPY ["src/Streetcode.Email.Application/Streetcode.Email.Application.csproj", "src/Streetcode.Email.Application/"]
COPY ["src/Streetcode.Email.Contracts/Streetcode.Email.Contracts.csproj", "src/Streetcode.Email.Contracts/"]
COPY ["src/Streetcode.Email.Infrastructure/Streetcode.Email.Infrastructure.csproj", "src/Streetcode.Email.Infrastructure/"]
COPY ["src/Streetcode.Email.WebApi/Streetcode.Email.WebApi.csproj", "src/Streetcode.Email.WebApi/"]

RUN dotnet restore \
	"src/Streetcode.Email.WebApi/Streetcode.Email.WebApi.csproj"
COPY . .

RUN dotnet publish \
	"src/Streetcode.Email.WebApi/Streetcode.Email.WebApi.csproj" \
	--configuration "$BUILD_CONFIGURATION" \
	--output /app/publish \
	--no-restore \
	/p:UseAppHost=false

FROM base AS final

COPY --from=build /app/publish .

USER $APP_UID

ENTRYPOINT ["dotnet", "Streetcode.Email.WebApi.dll"]
