# Evidencias P2C — Vulnerabilidades de Autenticación

Capturas a añadir en esta carpeta:

1. **`01-login-valido.png`** — Login exitoso con `user1` / `123456`, llegando al Dashboard.
2. **`02-login-predeterminado.png`** — Login exitoso con `admin` / `admin`, llegando al Dashboard.
3. **`03-login-invalido.png`** — Intento con credenciales incorrectas mostrando "Usuario/contraseña inválido".
4. **`04-codigo-vulnerable.png`** — Captura de `Controllers/AuthController.cs` resaltando la credencial hardcodeada y la consulta concatenada.

## Tabla de resultados

| # | Usuario | Contraseña | Resultado |
|---|---|---|---|
| 1 | admin | admin | Acceso concedido (credencial predeterminada) |
| 2 | user1 | 123456 | Acceso concedido (credencial válida) |
| 3 | foo | bar | Acceso denegado |
| 4 | `' OR '1'='1' --` | cualquiera | Acceso concedido como `admin` (bypass por SQL Injection) |

## Cómo reproducir

```bash
dotnet run --project VulnerableApp
```

Visitar `http://localhost:<puerto>/Auth/Login` y probar cada fila de la tabla anterior.
