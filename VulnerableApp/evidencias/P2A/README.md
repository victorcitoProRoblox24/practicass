# Evidencias P2A — Configuración Inicial

1. **`01-build.png`** — Terminal ejecutando `dotnet build` desde `VulnerableApp/` mostrando "Compilación correcta".
2. **`02-database.png`** — SQL Server Object Explorer (Visual Studio) o Azure Data Studio / SSMS conectado a `(localdb)\mssqllocaldb`, mostrando la base `VulnerableDb` creada.
3. **`03-tabla-users.png`** — Resultado de `SELECT * FROM Users` mostrando los 3 registros semilla (admin, user1, user2).
4. **`04-primer-commit.png`** — Salida de `git log --oneline` mostrando el commit "avance practica".

## Cómo reproducir rápidamente

```bash
cd VulnerableApp
dotnet build
sqlcmd -S "(localdb)\mssqllocaldb" -d VulnerableDb -Q "SELECT * FROM Users"
git log --oneline
```
