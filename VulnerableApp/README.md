# VulnerableApp

Aplicación ASP.NET Core MVC construida **deliberadamente vulnerable** con fines educativos, para las prácticas SEGG-U1-P2A a P2F (Seguridad en el Desarrollo de Aplicaciones — Unidad 1: Vulnerabilidades Web en .NET Core).

> ⚠️ Uso exclusivamente educativo en laboratorio local. No desplegar en un entorno público ni reutilizar el código como base de un proyecto real.

## Ramas

| Rama | Contenido |
|---|---|
| `master` | Versión **vulnerable** (P2A–P2E): SQL Injection, autenticación insegura, XSS almacenado, IDOR, contraseñas en texto plano. |
| `secure` | Versión **remediada** (P2F): consultas parametrizadas, contraseñas con hash BCrypt, salida codificada, autorización por ownership en la API. |

## Requisitos

- .NET SDK 10 (`dotnet --version`)
- SQL Server LocalDB (`sqllocaldb info` debe listar `MSSQLLocalDB`)

## Ejecución — versión vulnerable (`master`)

```bash
git checkout master
dotnet ef database update   # crea/actualiza VulnerableDb con Users.Password en texto plano
dotnet run
```

Endpoints de prueba:
- `/Search/Index?search=admin` — búsqueda; probar también `' OR '1'='1` (SQL Injection).
- `/Auth/Login` — probar `admin/admin`, `user1/123456`, y el bypass `' OR '1'='1' --` como usuario.
- `/Comment/Index` — probar `<script>alert('XSS')</script>`.
- `/api/user/{id}` y `/api/users` — sin autenticación, exponen todos los campos incl. `Password`.

> ⚠️ **Al cambiar de rama**, la tabla `__EFMigrationsHistory` de `VulnerableDb` no se revierte automáticamente: `dotnet ef database update` no detecta que la rama `secure` ya renombró la columna `Password` a `PasswordHash`, y simplemente reporta "ya está actualizada" aunque el esquema real no coincida con el modelo de `master`. Si acabas de estar en `secure` y vuelves a `master` (o viceversa), recrea la base para evitar errores en tiempo de ejecución (`Invalid column name...`):
> ```bash
> dotnet ef database drop --force
> dotnet ef database update
> ```

## Ejecución — versión segura (`secure`)

```bash
git checkout secure
dotnet ef database update   # aplica la migración SecureAuth (renombra Password -> PasswordHash)
dotnet run
```

Usuarios semilla (misma contraseña en texto plano que antes, ahora solo como valor de prueba — se valida contra el hash BCrypt):

| Usuario | Contraseña |
|---|---|
| admin | admin |
| user1 | 123456 |
| user2 | password |

### Comportamiento esperado tras la remediación

| Prueba | Resultado esperado |
|---|---|
| `search=' OR '1'='1` en `/Search/Index` | Sin resultados (ya no expone todos los usuarios) |
| Login con credenciales inválidas o bypass SQLi | "Credenciales inválidas", sin acceso |
| `<script>alert('XSS')</script>` en `/Comment/Index` | Se muestra como texto, no se ejecuta |
| `GET /api/user/{id}` sin sesión | `401 Unauthorized` |
| `GET /api/user/{id}` con sesión de **otro** usuario | `403 Forbidden` |
| `GET /api/user/{id}` y `GET /api/users` | Nunca incluyen `Password` ni `PasswordHash` |

## Documentación

- `docs/Informe_SEGG-U1-P2.md` — informe consolidado de instalación (P2A) y hallazgos P2B/P2C.
- `docs/Informe_SEGG-U1-P2DEF.md` — hallazgos P2D (XSS), P2E (IDOR) y comparativa de remediación P2F.
- `evidencias/P2A` … `P2F` — carpetas con instrucciones de qué capturar en cada práctica.
