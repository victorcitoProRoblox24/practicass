# Evidencias P2B — SQL Injection

Capturas a añadir en esta carpeta:

1. **`01-busqueda-legitima.png`** — Navegador en `https://localhost:<puerto>/Search/Index?search=admin` mostrando solo el usuario `admin`.
2. **`02-busqueda-manipulada.png`** — Navegador en `/Search/Index` con el campo de búsqueda `' OR '1'='1`, mostrando los 3 usuarios (incluye columna Balance).
3. **`03-codigo-vulnerable.png`** — Captura de `Controllers/SearchController.cs` resaltando la línea de concatenación de `query`.


## Cómo reproducir

```bash
dotnet run --project VulnerableApp
```
Luego visitar (ajustando el puerto que muestre la consola):

- `http://localhost:<puerto>/Search/Index?search=admin`
- `http://localhost:<puerto>/Search/Index?search=%27%20OR%20%271%27%3D%271` (equivalente URL-encoded de `' OR '1'='1`)
