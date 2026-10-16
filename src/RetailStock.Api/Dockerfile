# syntax=docker/dockerfile:1
# =====================================================================================
# RetailStock.Api - imagen multi-stage
#   Etapa "build":   SDK completo (~800 MB) para restaurar, compilar y publicar.
#   Etapa "runtime": solo el runtime de ASP.NET (~220 MB), sin compilador ni código fuente.
# Equivalente en Java: maven:3-eclipse-temurin para compilar y eclipse-temurin:21-jre para ejecutar.
#
# Construir (el contexto es la RAÍZ del repo):
#   docker build -f src/RetailStock.Api/Dockerfile -t retailstock-api .
# =====================================================================================

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# 1) Solo los archivos de proyecto primero: "dotnet restore" queda en su propia capa y se
#    reutiliza de la caché mientras no cambien las dependencias (aunque cambie el código).
COPY global.json Directory.Build.props ./
COPY src/RetailStock.Domain/RetailStock.Domain.csproj                 src/RetailStock.Domain/
COPY src/RetailStock.Application/RetailStock.Application.csproj       src/RetailStock.Application/
COPY src/RetailStock.Infrastructure/RetailStock.Infrastructure.csproj src/RetailStock.Infrastructure/
COPY src/RetailStock.Api/RetailStock.Api.csproj                       src/RetailStock.Api/
RUN dotnet restore src/RetailStock.Api/RetailStock.Api.csproj

# 2) Ahora el código y la publicación (Release, sin volver a restaurar)
COPY src/ src/
RUN dotnet publish src/RetailStock.Api/RetailStock.Api.csproj \
      --configuration Release \
      --no-restore \
      --output /app/publish \
      /p:UseAppHost=false

# -------------------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Las imágenes de .NET 8 escuchan en 8080 (puerto no privilegiado) por defecto
ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_RUNNING_IN_CONTAINER=true
EXPOSE 8080

COPY --from=build /app/publish .

# Usuario NO root: las imágenes de .NET 8 ya traen el usuario "app" (UID en $APP_UID).
# Si alguien explotara una vulnerabilidad, no tendría privilegios de root dentro del contenedor.
USER $APP_UID

ENTRYPOINT ["dotnet", "RetailStock.Api.dll"]
