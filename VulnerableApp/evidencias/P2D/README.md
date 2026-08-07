# Evidencias P2D — Cross-Site Scripting (XSS)

Capturas a añadir (rama `master`):

1. **`01-comentario-legitimo.png`** — `/Comment/Index` mostrando un comentario normal ya publicado.
2. **`02-payload-xss.png`** — Formulario con `<script>alert('XSS')</script>` recién enviado.
3. **`04-codigo-vulnerable.png`** — `Views/Comment/Index.cshtml` resaltando `@Html.Raw(comment)`.

## Cómo reproducir

```bash
git checkout master
dotnet run
```

Visitar `http://localhost:<puerto>/Comment/Index`, publicar un comentario normal, luego publicar exactamente:

```html
<script>alert('XSS')</script>
```

Recargar la página — el navegador debe mostrar la alerta emergente. Reiniciar la app (`Ctrl+C` y `dotnet run` de nuevo) para observar que los comentarios desaparecen (lista en memoria, no persistida).
