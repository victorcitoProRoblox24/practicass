# syntax=docker/dockerfile:1
#
# Imagen SOLO para el entorno de pruebas DAST (pentest-net, aislado).
# No se usa para desarrollo local: ahi VulnerableApp sigue corriendo con
# "dotnet run" directamente en el host, como en las practicas anteriores.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY VulnerableApp.csproj ./
RUN dotnet restore VulnerableApp.csproj

COPY . .
RUN dotnet publish VulnerableApp.csproj -c Release -o /app/publish /p:UseAppHost=false

# Empaqueta las migraciones de EF Core como un ejecutable autocontenido
# (efbundle). Permite aplicar el esquema + datos semilla contra el SQL
# Server del contenedor "db" sin necesitar el SDK ni la herramienta
# dotnet-ef en la imagen final de runtime.
RUN dotnet tool install --global dotnet-ef --version 10.0.10
ENV PATH="$PATH:/root/.dotnet/tools"
RUN dotnet restore VulnerableApp.csproj --runtime linux-x64
RUN dotnet ef migrations bundle \
    --project VulnerableApp.csproj \
    --configuration Release \
    --self-contained \
    --runtime linux-x64 \
    --output /app/publish/efbundle

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
COPY docker-entrypoint.sh .
RUN chmod +x docker-entrypoint.sh efbundle

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["./docker-entrypoint.sh"]
