# Informe — SonarQube en Docker + .NET Core 10

## 1. Resumen de lo realizado

- SonarQube Community Edition + PostgreSQL desplegados en Docker (`sonarqube-infra/docker-compose.yml`), accesibles en `http://localhost:9000`.
- Proyecto `MiAppNetCore` creado en SonarQube, con token de análisis (`PROJECT_ANALYSIS_TOKEN`) generado vía API.
- Solución ASP.NET Core 10 (`MiAppNetCore.slnx`) con dos proyectos:
  - `MiAppNetCore`: Web API con `Services/WeatherService.cs` y `Controllers/WeatherController.cs`, usando SQLite embebido.
  - `MiAppNetCore.Tests`: pruebas xUnit con cobertura via `coverlet.collector`.
- Se instaló `dotnet-sonarscanner` como herramienta global y se ejecutó el ciclo `begin` → `build` → `test` → `end` dos veces (antes y después de corregir).

## 2. Hallazgos identificados y corregidos

| # | Tipo | Regla | Ubicación | Descripción | Corrección aplicada |
|---|------|-------|-----------|-------------|----------------------|
| 1 | Security Hotspot (SQL Injection, HIGH) | `sql-injection` | `WeatherService.cs:73` | Concatenación directa del input del usuario en una query SQL (`LIKE '%' + cityName + '%'`) | Se reemplazó por un parámetro (`$namePattern`) usando `command.Parameters.AddWithValue` |
| 2 | Bug (Null reference) | `external_roslyn:CS8602` | `WeatherService.cs` (`GetCityWeatherSummary`) | Se accedía a `match.Name` sin comprobar si la búsqueda devolvió resultados | Se agregó una validación `if (match is null)` antes de usar el resultado |
| 3 | Code Smell | `csharpsquid:S1481` / `CS0219` | `WeatherService.cs` (`GetWeatherDescription`) | Variable local `unitLabel` declarada y nunca usada | Se eliminó la variable |
| 4 | Code Smell | `csharpsquid:S2325` (x2) | `WeatherService.cs` | Métodos que no usan estado de instancia | Se marcaron como `static` |
| 5 | Code Smell | `csharpsquid:S6966` | `Program.cs` | Uso de `app.Run()` en vez de la versión asíncrona | Cambiado a `await app.RunAsync();` |
| 6 | Code Smell | `csharpsquid:S1118` / `ASP0027` | `Program.cs` | Clase `public partial class Program { }` innecesaria (no se usa `WebApplicationFactory` en las pruebas) | Se eliminó la declaración |

**Resultado del segundo análisis:** `bugs: 0`, `vulnerabilities: 0`, `code_smells: 0`, `security_hotspots: 0`, cobertura `73.7%` (13→15 pruebas, todas en verde).

## 3. Preguntas de reflexión

### 3.1 ¿Qué diferencia hay entre un Security Hotspot y un Bug en SonarQube?

Un **Bug** es un defecto que SonarQube considera que **ya produce (o producirá con certeza) un comportamiento incorrecto** en tiempo de ejecución — por ejemplo, una desreferencia de un valor posiblemente nulo, una condición que nunca se cumple, o un recurso que no se libera. No requiere criterio humano: si el patrón está presente, es un error.

Un **Security Hotspot**, en cambio, marca código que **usa una funcionalidad sensible desde el punto de vista de seguridad** (una query SQL dinámica, un `Random` no criptográfico, deshabilitar la verificación de certificados, etc.), pero cuya explotabilidad real depende del contexto: de dónde viene el dato, cómo se usa después, si hay otra capa de validación, etc. Por eso un hotspot **no es automáticamente una vulnerabilidad confirmada**: un humano debe revisarlo y decidir si es seguro (`Safe`), si hay que corregirlo (`Fixed` tras aplicar la corrección) o si se reconoce el riesgo temporalmente (`Acknowledged`). Una **Vulnerability** (tercera categoría) es lo que resulta cuando ese hotspot, u otro patrón, se confirma como explotable de forma directa por el análisis estático, sin necesidad de revisión humana previa.

En resumen: Bug = error funcional cierto; Vulnerability = riesgo de seguridad confirmado; Security Hotspot = riesgo de seguridad que necesita revisión humana para confirmarse.

### 3.2 ¿Por qué es inseguro concatenar directamente el input del usuario en una query SQL? Explica el ataque con un ejemplo de payload.

Cuando el input del usuario se concatena como texto dentro del comando SQL, el motor de base de datos no puede distinguir entre "código SQL que el desarrollador escribió" y "datos que el usuario envió": todo llega como un único string que se interpreta y ejecuta literalmente. Un atacante puede entonces incluir fragmentos de SQL válidos dentro del input para alterar la consulta original.

En este proyecto, el código vulnerable era:

```csharp
command.CommandText =
    "SELECT Id, Name, TemperatureC FROM Cities WHERE Name LIKE '%" + cityName + "%'";
```

Si un usuario envía como `cityName` el payload:

```
%' UNION SELECT Id, Username, PasswordHash FROM Users --
```

la consulta final ejecutada por la base de datos se convierte en:

```sql
SELECT Id, Name, TemperatureC FROM Cities WHERE Name LIKE '%%' UNION SELECT Id, Username, PasswordHash FROM Users --%'
```

El `UNION SELECT` permite combinar los resultados de la tabla `Cities` con los de la tabla `Users` (incluso credenciales), y los `--` comentan el resto de la query original para que no cause un error de sintaxis. Con variantes de este ataque un atacante puede leer datos de otras tablas, saltarse autenticación (`' OR '1'='1`), o incluso modificar/borrar datos si el usuario de la base de datos tiene permisos de escritura.

La corrección (parámetros: `command.Parameters.AddWithValue("$namePattern", $"%{cityName}%")`) evita esto porque el motor de base de datos recibe el SQL y los datos por canales separados: el valor del usuario nunca se interpreta como sintaxis SQL, sin importar qué caracteres contenga.

### 3.3 ¿Qué es el Code Coverage y por qué SonarQube lo considera relevante para la seguridad?

El Code Coverage es el porcentaje de líneas/ramas del código que se ejecutan al correr la suite de pruebas automatizadas. No mide si el código es "correcto", sino cuánta superficie del código está siendo efectivamente puesta a prueba.

Es relevante para la seguridad porque el código que nunca se ejecuta en una prueba es código que nadie está verificando de forma sistemática: cambios futuros (refactors, correcciones de otros bugs) pueden reintroducir una vulnerabilidad ya corregida, o romper una validación de seguridad (autenticación, sanitización de input, manejo de permisos) sin que nada lo detecte hasta que ocurre en producción. Baja cobertura en rutas críticas (autenticación, manejo de pagos, validación de entrada) es en sí misma un indicador de riesgo, porque ese código depende únicamente de la revisión manual para mantenerse seguro con el tiempo. Por eso SonarQube lo muestra junto a bugs y vulnerabilidades: cobertura alta no garantiza seguridad, pero cobertura baja sí es una señal de que los controles de seguridad existentes son frágiles ante cambios futuros.

### 3.4 Si un colega sube el token de SonarQube a GitHub por error, ¿qué pasos seguirías para mitigar el riesgo?

1. **Revocar el token inmediatamente** desde SonarQube (`My Account → Security → Revoke`), incluso antes de limpiar el repositorio — esto invalida el token al instante sin importar dónde haya quedado copiado.
2. **Generar un token nuevo** y actualizarlo en el lugar correcto (variable de entorno, secret manager, secreto de CI/CD), nunca en el código.
3. **Eliminar el token del historial de Git**, no solo del archivo actual: un `git revert` o borrar la línea en un commit nuevo no lo quita del historial. Hay que reescribir el historial (`git filter-repo` o BFG Repo-Cleaner) y forzar el push, coordinando con el equipo porque esto reescribe hashes de commits.
4. **Revisar los logs de acceso/auditoría de SonarQube** (si están disponibles) para verificar si el token comprometido fue usado por alguien no autorizado mientras estuvo expuesto.
5. **Si el repositorio es público o el token pudo haber sido indexado** (por bots que escanean GitHub buscando secretos), tratar el incidente como una posible exposición real, no solo teórica.
6. **Prevención a futuro**: agregar el patrón del token a `.gitignore`/`git-secrets`, activar el escaneo de secretos de GitHub (secret scanning / push protection), y reforzar en el equipo que los tokens se manejan solo como variables de entorno o en un gestor de secretos.

### 3.5 ¿Cómo integrarías SonarQube en un pipeline CI/CD de GitHub Actions? Describe los pasos a alto nivel.

1. **Guardar el token como secreto del repositorio** (`Settings → Secrets and variables → Actions`), por ejemplo `SONAR_TOKEN`, junto con la URL del servidor si no es la instancia pública de SonarCloud (`SONAR_HOST_URL`).
2. **Disparar el workflow** en los eventos relevantes: `push` a la rama principal y `pull_request` hacia ella, para que cada cambio se analice antes de integrarse.
3. **Configurar el job**:
   - Checkout del código con `actions/checkout@v4` (con `fetch-depth: 0` para que SonarQube pueda calcular correctamente información como nuevas líneas de código respecto a la rama base).
   - Instalar el SDK de .NET correspondiente (`actions/setup-dotnet@v4`).
   - Instalar `dotnet-sonarscanner` como herramienta (o cachearla entre corridas).
4. **Ejecutar el mismo ciclo que en local**: `dotnet sonarscanner begin` (con `sonar.token`, `sonar.host.url` y las rutas de reportes de cobertura/tests como secretos/variables), `dotnet build`, `dotnet test --collect:"XPlat Code Coverage"`, y `dotnet sonarscanner end`.
5. **Usar el Quality Gate como gate del pipeline**: SonarQube expone una API (`api/qualitygates/project_status`) o el propio scanner puede esperar el resultado; si el Quality Gate falla (por ejemplo, nuevos bugs, cobertura de código nuevo por debajo del umbral, o hotspots sin revisar), el job de GitHub Actions debe fallar para bloquear el merge del pull request.
6. **Mostrar el resultado en el PR**: con la integración de SonarQube/SonarCloud con GitHub, los hallazgos aparecen como comentarios o checks directamente en el pull request, dando feedback al desarrollador sin que tenga que entrar al dashboard manualmente.
