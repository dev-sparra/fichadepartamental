# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copiar el .xlsm embebido como EmbeddedResource desde docs/ (referenciado por el csproj con ..\..\..\)
COPY docs/ficha_departamental_gobernanza.xlsm /docs/ficha_departamental_gobernanza.xlsm

# Copiar solo los .csproj primero (mejor cache de Docker)
COPY src/backend/PortalNacionalGobernanzaMusical.API/PortalNacionalGobernanzaMusical.API.csproj ./PortalNacionalGobernanzaMusical.API/
COPY src/backend/PortalNacionalGobernanzaMusical.Application/PortalNacionalGobernanzaMusical.Application.csproj ./PortalNacionalGobernanzaMusical.Application/
COPY src/backend/PortalNacionalGobernanzaMusical.Domain/PortalNacionalGobernanzaMusical.Domain.csproj ./PortalNacionalGobernanzaMusical.Domain/
COPY src/backend/PortalNacionalGobernanzaMusical.Infrastructure/PortalNacionalGobernanzaMusical.Infrastructure.csproj ./PortalNacionalGobernanzaMusical.Infrastructure/
COPY src/backend/PortalNacionalGobernanzaMusical.Persistence/PortalNacionalGobernanzaMusical.Persistence.csproj ./PortalNacionalGobernanzaMusical.Persistence/
COPY src/backend/PortalNacionalGobernanzaMusical.Shared/PortalNacionalGobernanzaMusical.Shared.csproj ./PortalNacionalGobernanzaMusical.Shared/
COPY src/backend/*.sln ./

RUN dotnet restore PortalNacionalGobernanzaMusical.sln

# Copiar el resto del codigo fuente
COPY src/backend/ ./

# Publicar el proyecto API
RUN dotnet publish PortalNacionalGobernanzaMusical.API/PortalNacionalGobernanzaMusical.API.csproj \
    -c Release \
    -o /app \
    --no-restore \
    --self-contained false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

# Copiar la app publicada
COPY --from=build /app ./

# Render inyecta PORT en runtime (default 10000); ASP.NET Core escucha ahi.
# Si PORT no esta definido, cae a 8080.
ENV ASPNETCORE_ENVIRONMENT=Staging
ENV DOTNET_PRINT_HOST_PROCESS_ID=1

EXPOSE 8080

# Construir la URL de escucha en runtime respetando $PORT de Render.
ENTRYPOINT ["sh", "-c", "export ASPNETCORE_URLS=http://+:${PORT:-8080} && dotnet PortalNacionalGobernanzaMusical.API.dll"]