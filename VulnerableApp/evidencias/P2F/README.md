# Evidencias P2F — Remediación de Vulnerabilidades OWASP

Capturas a añadir (comparar rama `master` vs. rama `secure`):

1. **`01-rama-secure.png`** — `git branch` o `git log --oneline --graph --all` mostrando la rama `secure` creada a partir de `master`.
2. **`02-sqli-antes-despues.png`** — Búsqueda con `' OR '1'='1` en `master` (devuelve todo) vs. en `secure` (devuelve vacío).

## Cómo reproducir la comparación

```bash
# Versión vulnerable
git checkout master
dotnet ef database update
dotnet run
# probar los payloads de P2B, P2C, P2D, P2E

# Versión remediada
git checkout secure
dotnet ef database update    # aplica la migración SecureAuth
dotnet run
# repetir los mismos payloads y confirmar que ya no funcionan
```

Ver la tabla de validación completa (con resultados ya verificados) en `docs/Informe_SEGG-U1-P2DEF.md`, sección 3.2.
