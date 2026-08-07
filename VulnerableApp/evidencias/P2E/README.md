# Evidencias P2E — IDOR

Capturas a añadir (rama `master`, Postman o navegador):

1. **`01-get-user-1.png`** — `GET /api/user/1` mostrando el JSON completo (incluye `password`).
2. **`02-get-user-2.png`** — `GET /api/user/2` (dato de un usuario distinto, sin haber iniciado sesión).
3. **`03-get-users.png`** — `GET /api/users` mostrando el listado completo.
4. **`04-codigo-vulnerable.png`** — `Controllers/ApiController.cs` resaltando la ausencia de verificación de sesión/ownership y el campo `user.Password` en la respuesta.

## Cómo reproducir

```bash
git checkout master
dotnet run
```

Con Postman, navegador o `curl`, sin ninguna cookie de sesión:

```
GET http://localhost:<puerto>/api/user/1
GET http://localhost:<puerto>/api/user/2
GET http://localhost:<puerto>/api/user/3
GET http://localhost:<puerto>/api/users
```

Todas deben responder `200 OK` con datos completos, sin pedir autenticación.
