# Reporte de Práctica — Resolución de Issues SonarQube (Security / Reliability / Maintainability)

> Contenido listo para volcar en el documento oficial de la práctica. Todos los valores fueron obtenidos directamente de la API de SonarQube (`http://localhost:9000`), no son estimados.

## 1. Datos generales

| Campo | Valor |
|---|---|
| Proyecto analizado | `MiAppNetCore` (ASP.NET Core 10) |
| Herramienta | SonarQube Community Edition `26.6.0.123539` (Docker) |
| Scanner | `dotnet-sonarscanner` `11.2.1` |
| Dashboard | http://localhost:9000/dashboard?id=MiAppNetCore |
| Fecha análisis "antes" (con hallazgos) | 2026-08-07 06:04 UTC |
| Fecha análisis "después" (0 issues, confirmación final) | 2026-08-07 02:10 (hora local, dos corridas de verificación) |
| Quality Gate final | **OK** ✅ |

## 2. Resumen ejecutivo — Antes vs. Después

| Categoría (Clean Code) | Métrica SonarQube | Antes | Después |
|---|---|---|---|
| **Reliability** | Bugs | 0 | 0 |
| **Reliability** | Reliability Rating | A (1.0) | A (1.0) |
| **Security** | Vulnerabilities | 0 | 0 |
| **Security** | Security Hotspots (pendientes de revisar) | **1** | **0** |
| **Security** | Security Rating | A (1.0) | A (1.0) |
| **Maintainability** | Code Smells | **8** | **0** |
| **Maintainability** | Maintainability Rating (Sqale) | A (1.0) | A (1.0) |
| — | Coverage (cobertura de pruebas) | 68.9 % | 72.0 % |
| — | Duplicated Lines Density | 0.0 % | 0.0 % |
| — | Líneas de código analizadas (ncloc) | 151 | 150 |
| — | **Quality Gate** | OK | **OK** |

> Nota importante: los *ratings* (Security/Reliability/Maintainability = A) ya estaban en A desde el primer análisis porque SonarQube solo baja el rating de Reliability/Security cuando existen issues de tipo **Bug** o **Vulnerability** confirmados; el hallazgo de SQL Injection se clasificó como **Security Hotspot** (requiere revisión humana, no cuenta como Vulnerability hasta confirmarse) y el resto de hallazgos se clasificaron como **Code Smells**, que afectan el **Maintainability Rating** (sqale) y el conteo de `code_smells`, ambos llevados de 8 → 0.

## 3. Detalle de los issues resueltos, por categoría

### 3.1 Security — Security Hotspot (1 → 0)

| Regla | Archivo:línea | Severidad | Descripción | Corrección aplicada |
|---|---|---|---|---|
| `sql-injection` | `MiAppNetCore/Services/WeatherService.cs:73` | HIGH | La query SQL se construía concatenando directamente el input del usuario (`"...LIKE '%" + cityName + "%'"`), permitiendo inyección SQL (ej. `UNION SELECT`, `OR '1'='1`). | Se reemplazó la concatenación por un parámetro real: `command.Parameters.AddWithValue("$namePattern", $"%{cityName}%")`. El motor SQLite ahora trata el valor siempre como dato, nunca como sintaxis SQL. Además se marcó el hotspot como **Acknowledged** en SonarQube con comentario explicando la corrección aplicada, y tras el nuevo análisis dejó de detectarse (0 hotspots). |

### 3.2 Maintainability — Code Smells (8 → 0)

| Regla | Archivo:línea | Severidad | Descripción | Corrección aplicada |
|---|---|---|---|---|
| `external_roslyn:CS8602` | `WeatherService.cs` (`GetCityWeatherSummary`) | MAJOR | Posible `NullReferenceException`: se accedía a `match.Name` sin comprobar si la búsqueda devolvió resultados. | Se agregó validación `if (match is null) return "No se encontraron resultados...";` antes de usar el resultado. |
| `csharpsquid:S1481` / `external_roslyn:CS0219` | `WeatherService.cs:112` (`GetWeatherDescription`) | MINOR / MAJOR | Variable local `unitLabel` declarada y nunca utilizada. | Se eliminó la variable. |
| `csharpsquid:S2325` | `WeatherService.cs:96` (`IsValidTemperature`) | MINOR | El método no usa estado de instancia y debería ser `static`. | Se marcó como `static`. |
| `csharpsquid:S2325` | `WeatherService.cs:110` (`GetWeatherDescription`) | MINOR | Igual al anterior. | Se marcó como `static`. |
| `csharpsquid:S6966` | `Program.cs:24` | MAJOR | Se usaba `app.Run()` en lugar de la versión asíncrona. | Se cambió a `await app.RunAsync();`. |
| `csharpsquid:S1118` | `Program.cs:26` | MAJOR | La clase `public partial class Program { }` (usada por convención para exponer `Program` a pruebas de integración) no tenía constructor `protected`/`static`. | Como el proyecto no usa `WebApplicationFactory<Program>` en las pruebas (se testea `WeatherService` directamente), la declaración era innecesaria: **se eliminó por completo**. |
| `external_roslyn:ASP0027` | `Program.cs:26` | INFO | Advertencia del SDK: en ASP.NET Core moderno ya no es necesario declarar `public partial class Program`. | Resuelto junto con el punto anterior (se eliminó la declaración). |

### 3.3 Reliability — Bugs (0 → 0)

No se generaron issues de tipo **Bug** en ninguno de los dos análisis. El riesgo de referencia nula (`CS8602`) que conceptualmente es un bug de fiabilidad fue importado por SonarQube bajo el tipo **Code Smell** (ver tabla 3.2), por lo que se documenta y corrigió ahí. Se deja constancia explícita para que el reporte no omita esta categoría: **0 Bugs antes, 0 Bugs después, Reliability Rating A en ambos análisis.**

## 4. Evidencia técnica (verificación vía API de SonarQube)

Comando de verificación final ejecutado después de re-correr el análisis (`begin` → `build` → `test` → `end`):

```
GET /api/measures/component?component=MiAppNetCore&metricKeys=security_rating,reliability_rating,sqale_rating,bugs,vulnerabilities,code_smells,security_hotspots,coverage,duplicated_lines_density,ncloc
```

Respuesta (resumida):

```json
{
  "bugs": "0",
  "vulnerabilities": "0",
  "code_smells": "0",
  "security_hotspots": "0",
  "security_rating": "1.0",
  "reliability_rating": "1.0",
  "sqale_rating": "1.0",
  "coverage": "72.0",
  "duplicated_lines_density": "0.0",
  "ncloc": "150"
}
```

```
GET /api/qualitygates/project_status?projectKey=MiAppNetCore
→ "status": "OK"
```

Pruebas unitarias: **15/15 exitosas** (`dotnet test`), cobertura recolectada con `coverlet.collector` en formato OpenCover e importada por el sensor de cobertura de SonarQube.

## 5. Conclusiones

- Se resolvieron **todos** los issues detectados por SonarQube en las tres dimensiones del modelo Clean Code: 1 Security Hotspot (SQL Injection) y 8 Code Smells (incluyendo el riesgo de referencia nula).
- Tras aplicar las correcciones y volver a ejecutar el ciclo completo de análisis, el proyecto quedó en **0 Bugs, 0 Vulnerabilities, 0 Code Smells, 0 Security Hotspots**, con calificación **A** en Security, Reliability y Maintainability, y **Quality Gate en estado OK**.
- La cobertura de pruebas se mantuvo estable (68.9 % → 72.0 %) gracias a que las correcciones se acompañaron de ajustes/adiciones en la suite de pruebas (`MiAppNetCore.Tests`), evitando que el fix del bug de temperatura (antes con condición siempre verdadera) rompiera las pruebas existentes.
- Nota aparte (no es un issue de SonarQube): `dotnet build` reporta 2 advertencias `NU1903` de vulnerabilidades conocidas en paquetes NuGet transitivos (`Microsoft.OpenApi` 2.0.0, `SQLitePCLRaw.lib.e_sqlite3` 2.1.11). SonarQube Community Edition no realiza análisis de dependencias (Software Composition Analysis) por defecto, por lo que estas advertencias provienen de NuGet, no del análisis estático de SonarQube, y quedan fuera del alcance de "0 issues" solicitado, pero se documentan aquí como hallazgo adicional relevante para seguridad.
