FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app

# adding curl and gpg for healthcheck
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
    curl \
    gpg \
    && rm -rf /var/lib/apt/lists/*
EXPOSE 5000
EXPOSE 5001
EXPOSE 80
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG Configuration=Release
WORKDIR /src

# restoring application dependencies
COPY ./Streetcode/Streetcode.WebApi/*.csproj ./Streetcode/Streetcode.WebApi/
COPY ./Streetcode/Streetcode.BLL/*.csproj ./Streetcode/Streetcode.BLL/
COPY ./Streetcode/Streetcode.DAL/*.csproj ./Streetcode/Streetcode.DAL/
COPY ./Streetcode.Email/src/Streetcode.Email.Contracts/*.csproj ./Streetcode.Email/src/Streetcode.Email.Contracts/
RUN dotnet restore ./Streetcode/Streetcode.WebApi/Streetcode.WebApi.csproj

# copying application sources and building the Web API project
COPY ./Streetcode/ ./Streetcode/
COPY ./Streetcode.Email/src/Streetcode.Email.Contracts/ ./Streetcode.Email/src/Streetcode.Email.Contracts/
WORKDIR /src/Streetcode/Streetcode.WebApi
RUN dotnet build Streetcode.WebApi.csproj -c "$Configuration" -o /app/build --no-restore

# publishing application
FROM build AS publish
ARG Configuration=Release
RUN dotnet publish Streetcode.WebApi.csproj -c "$Configuration" -o /app/publish --no-restore /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish ./

LABEL atom="Streetcode"
ENTRYPOINT ["dotnet", "Streetcode.WebApi.dll"]
