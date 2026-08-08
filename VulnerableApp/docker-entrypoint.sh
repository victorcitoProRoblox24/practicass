#!/bin/sh
# Aplica las migraciones de EF Core (esquema + datos semilla) contra el SQL
# Server del contenedor "db" antes de arrancar la app. Es idempotente: si el
# esquema ya esta al dia, no hace nada. Reintenta porque el healthcheck de
# "db" solo garantiza que SQL Server acepta conexiones, no que el motor
# termino de inicializar por completo en el primer intento.
set -e

echo "Aplicando migraciones de EF Core (efbundle)..."
attempt=0
until ./efbundle --connection "$ConnectionStrings__DefaultConnection"; do
    attempt=$((attempt + 1))
    if [ "$attempt" -ge 10 ]; then
        echo "No se pudieron aplicar las migraciones tras $attempt intentos." >&2
        exit 1
    fi
    echo "Fallo al migrar (intento $attempt), reintentando en 5s..."
    sleep 5
done

echo "Migraciones aplicadas. Iniciando VulnerableApp..."
exec dotnet VulnerableApp.dll
