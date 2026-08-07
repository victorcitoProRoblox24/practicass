# SEGG-U2-P4H-1 — Plataforma de Observabilidad (Grafana + Loki + Promtail)

Esta carpeta contiene toda la infraestructura ya lista. VulnerableApp **no se toca ni se
containeriza**: sigue corriendo local con `dotnet run`, tal como en SEGG-U2-P3G-1. Docker
solo levanta Grafana, Loki y Promtail, que leen la carpeta `VulnerableApp/Logs` mediante un
bind mount de solo lectura.

```
observability-infra/
├── docker-compose.yml
├── grafana/
│   └── provisioning/datasources/datasource.yml   <- Loki ya queda dado de alta solo
└── promtail/
    └── promtail-config.yml                       <- scrapea Logs/*.txt y arma labels
```

## 0. Qué ya se hizo por ti

- `docker-compose.yml`: servicios `grafana`, `loki`, `promtail` completos (imágenes, puertos,
  volúmenes, red `observability`). Se quitó la clave `version:` del template porque Compose v2
  la marca como obsoleta.
- Bind mount: `../VulnerableApp/Logs:/var/log/vulnerableapp:ro` en el servicio `promtail`. Es
  de **solo lectura** porque Promtail únicamente necesita leer los archivos, nunca escribirlos;
  así se evita que un contenedor pueda corromper los logs de la app.
- `promtail-config.yml`: ya incluye el **Reto** resuelto — parsea cada línea con `regex`,
  clasifica el mensaje en `category: security | audit | vulnerableapp` con un `template`, y
  promueve `level` y `category` a labels con `labels`.
- Datasource de Grafana → Loki: provisionado automáticamente (`grafana/provisioning/...`), no
  tienes que darlo de alta a mano, solo verificarlo (Paso 7).
- Contenedores levantados con `docker compose up -d` (verás el `docker ps` correspondiente más
  abajo, esa misma salida es una de las evidencias que pide la rúbrica).

Lo único que **tú** debes hacer es: correr VulnerableApp, generar actividad, y tomar las
capturas. Abajo tienes exactamente qué clic dar y qué pegar en el documento.

---

## 1. Levantar / verificar la infraestructura

Si alguna vez necesitas repetirlo:

```powershell
cd "observability-infra"
docker compose up -d
docker compose ps
```

Para bajarla: `docker compose down` (los datos de Grafana/Loki quedan en los volúmenes
`grafana_data` y `loki_data`, así que no se pierden entre reinicios).

**📸 Evidencia — `docker ps`**: captura de pantalla de la terminal mostrando los 3 contenedores
(`grafana`, `loki`, `promtail`) en estado `Up`.

---

## 2. Ejecutar VulnerableApp

En otra terminal, **fuera de Docker**, igual que en la práctica anterior:

```powershell
cd "VulnerableApp"
dotnet run
```

La app queda en `http://localhost:5277`. Abre esa URL en el navegador.

**📸 Evidencia — "VulnerableApp generando registros"**: captura de la terminal de `dotnet run`
mostrando las líneas `[INF] ... Now listening on...`, `Application started`, etc.

---

## 3. Generar actividad (autenticación, CRUD, warnings, errores)

Con la app corriendo, realiza en el navegador, en este orden, para que tengas de todo tipo de
evento en el log:

| # | Acción | URL / Pasos | Qué genera |
|---|--------|-------------|------------|
| 1 | Home | `http://localhost:5277/` | INF general |
| 2 | Búsqueda | `http://localhost:5277/Search?search=prueba` | INF general |
| 3 | Login fallido | `http://localhost:5277/Auth/Login` → usuario `admin`, password incorrecto | WRN `AuthEvent:LoginFailed` |
| 4 | Login correcto | `http://localhost:5277/Auth/Login` → credenciales válidas (usuario que hayas creado/sembrado en la BD) | INF `AuthEvent:LoginSuccess` |
| 5 | Dashboard | Tras login exitoso, se redirige solo | INF `Auth.Dashboard` |
| 6 | Logout | `http://localhost:5277/Auth/Logout` | INF `AuthEvent:Logout` |
| 7 | Comentario válido (CRUD) | `http://localhost:5277/Comment` → escribe un comentario y envíalo | INF `Comment.AddComment` |
| 8 | Comentario vacío | Envía el formulario de comentario sin texto | WRN "Intento de agregar comentario vacío" |
| 9 | API CRUD | `http://localhost:5277/api/users` y `http://localhost:5277/api/user/1` | INF `Api.` (consulta a BD) |
| 10 | Excepción controlada | `http://localhost:5277/Search?search=__CONTROLLED_EXCEPTION_TEST__` | ERR capturado |
| 11 | Excepción no controlada | Login con usuario `__UNCONTROLLED_EXCEPTION_TEST__` | ERR/500 vía `ExceptionHandlingMiddleware` |

**📸 Evidencia — "archivos de la carpeta Logs"**: captura del Explorador de Windows o de
`Get-ChildItem VulnerableApp\Logs` mostrando el/los `log-*.txt` con fecha reciente, y otra
captura abriendo el archivo para mostrar las líneas nuevas.

**📸 Evidencia — Seq**: abre Seq (`http://localhost:5341` si sigue igual que en P3G-1),
captura los mismos eventos (login, comentario, error) ya visibles ahí.

---

## 4. Verificar Grafana + Loki (Paso 7)

1. Abre `http://localhost:3000` (usuario `admin`, password `admin`; Grafana pedirá
   cambiar la contraseña la primera vez, puedes omitirlo con "Skip" para la práctica).
2. Ve a **Connections → Data sources**. Debe aparecer **Loki** ya creado (viene del
   provisioning), apuntando a `http://loki:3100`.
3. Entra al datasource Loki y da clic en **Save & test** → debe decir "Data source
   successfully connected".

**📸 Evidencia — "Grafana con Loki configurado"**: captura de esa pantalla con el mensaje de
conexión exitosa.

4. Ve a **Explore** (ícono de brújula), selecciona el datasource **Loki**, y ejecuta:

```logql
{job="vulnerableapp"}
```

Deberías ver todas las líneas que generaste en el paso 3.

---

## 5. Reto — filtrar por labels (Security / Audit / VulnerableApp)

En **Explore**, con el datasource Loki, prueba estas consultas LogQL:

```logql
# Todo lo que generó la app
{job="vulnerableapp"}

# Solo eventos de autenticación (login, logout, dashboard, verificación BCrypt)
{job="vulnerableapp", category="security"}

# Solo auditoría: altas de comentarios y llamadas a la API/BD
{job="vulnerableapp", category="audit"}

# Solo el resto (home, búsqueda, health general)
{job="vulnerableapp", category="vulnerableapp"}

# Auditoría que además sea error
{job="vulnerableapp", category="audit", level="ERR"}
```

**📸 Evidencia — "consulta filtrada mediante labels"**: captura de la consulta
`{job="vulnerableapp", category="audit"}` en Grafana Explore mostrando solo esas líneas.

---

# Textos para pegar en el documento

## Actividad de análisis — tabla comparativa

| Característica | Seq | Grafana + Loki |
|---|---|---|
| Búsqueda textual | Motor propio muy fuerte en búsqueda estructurada sobre propiedades (SQL-like: `Usuario = 'admin'`), pensado para logs de .NET/Serilog | LogQL: filtra primero por labels (rápido, indexado) y luego con line filters (`\|=`, `\|~`, `!=`) sobre el texto; menos "amigable" pero muy potente combinando ambos |
| Dashboards | Limitado; Seq es sobre todo un visor/buscador de eventos, no un motor de dashboards | Punto fuerte de Grafana: paneles, gráficas de tasa de errores, tableros combinando múltiples fuentes (no solo logs) |
| Alertas | Soporta alertas básicas sobre señales/consultas guardadas | Grafana Alerting: reglas más flexibles, múltiples canales de notificación (correo, Slack, Teams, webhook), umbrales sobre series temporales derivadas de los logs |
| Consultas | Lenguaje propio orientado a propiedades estructuradas de Serilog (`@Level = 'Error'`) | LogQL (inspirado en PromQL): combina selección por labels + filtros de línea + funciones de agregación (`rate()`, `count_over_time()`, etc.) |
| Visualización | Lista de eventos expandibles, muy legible para depurar un caso puntual | Paneles configurables (series de tiempo, tablas, logs panel), mejor para ver tendencias y correlacionar con métricas |
| Uso principal | Debugging puntual durante desarrollo: "¿qué pasó en esta petición?" | Observabilidad de plataforma: correlacionar logs con métricas/traces, monitoreo continuo y alertamiento en producción |

## Reto — respuestas

**¿Qué modificaciones realizó en Promtail?**

Se agregó un `pipeline_stages` de tres etapas en `promtail-config.yml` sin tocar el código de
VulnerableApp:
1. Un stage `regex` que parsea cada línea del formato de salida de Serilog
   (`timestamp [LEVEL] [cid:...] mensaje`) y extrae `timestamp`, `level`, `correlation_id` y
   `message`.
2. Un stage `template` que evalúa el contenido de `message` con expresiones regulares
   (`regexMatch`) y calcula un campo `category` con tres valores posibles: `security` (líneas
   de `Auth.*` / `AuthEvent:*` / verificación BCrypt), `audit` (`Comment.*`, `Api.*`, consultas
   `Executed DbCommand`/INSERT/UPDATE/DELETE) o `vulnerableapp` (todo lo demás, por ejemplo
   Home o Search).
3. Un stage `labels` que promueve `level` y `category` de campos extraídos a **labels** reales
   de Loki, y un stage `timestamp` para que Loki use la hora real del log en vez de la hora de
   ingesta.

**¿Cómo utiliza Loki las labels?**

Loki indexa únicamente las labels (no el texto completo de la línea, como sí hace
Elasticsearch). Cada combinación única de labels forma un "stream" independiente. Al consultar
con `{job="vulnerableapp", category="audit"}`, Loki usa ese índice de labels para ubicar
directamente los streams relevantes sin escanear todos los logs, y solo después aplica
(opcionalmente) filtros de texto sobre esas líneas ya acotadas. Por eso es importante no crear
labels de cardinalidad muy alta (como un `correlation_id` distinto por request): se dejó como
campo extraído, no como label, precisamente para no explotar el número de streams.

**¿Qué ventajas ofrecen para realizar consultas con LogQL?**

- Las consultas son mucho más rápidas porque el filtrado inicial por label es indexado,
  a diferencia de recorrer todo el texto.
- Permiten aislar de forma limpia distintos dominios del mismo archivo de log (seguridad,
  auditoría, aplicación general) sin necesidad de que la aplicación escriba a archivos
  separados ni de modificar su código.
- Se pueden combinar varias labels en una sola consulta (`category="audit"` + `level="ERR"`)
  para responder preguntas muy específicas, como "errores durante operaciones de auditoría".
- Habilitan agregaciones (`count_over_time`, `rate`) agrupadas por label, útiles para
  dashboards y alertas (por ejemplo, tasa de eventos `category="security"` con `level="WRN"`
  como proxy de intentos de login fallidos).

## Pregunta de reflexión

¿Qué ventajas ofrece incorporar una plataforma de observabilidad sin modificar la aplicación
existente? Explique cómo esta arquitectura facilita la evolución de un sistema en producción.

> Incorporar Grafana, Loki y Promtail sin tocar VulnerableApp demuestra el principio de
> **separación de responsabilidades**: la aplicación solo se encarga de generar buenos logs
> (estructurados, con correlation id, con nivel adecuado) y la plataforma de observabilidad se
> encarga de recolectarlos, indexarlos y visualizarlos. Esto reduce el riesgo de la operación:
> no hay que recompilar, volver a probar ni desplegar la aplicación para ganar capacidades de
> monitoreo, lo que en producción significa cero downtime y cero riesgo de introducir un bug
> nuevo solo por "agregar observabilidad". Promtail actúa como un adaptador externo que lee los
> archivos ya existentes vía bind mount, por lo que la fuente de verdad (los logs) no cambia.
>
> Esta arquitectura también facilita la evolución del sistema: si mañana se decide migrar
> VulnerableApp a contenedores, a Kubernetes, o cambiar el formato de log, basta con ajustar la
> configuración de Promtail (el `pipeline_stages`) o apuntar el agente al nuevo destino de
> logs, sin que el equipo de desarrollo tenga que involucrarse. De igual forma, se pueden
> agregar más fuentes (métricas con Prometheus, trazas con Tempo) al mismo Grafana sin volver a
> tocar la aplicación, construyendo observabilidad de forma incremental y desacoplada del ciclo
> de vida del código de negocio.

---

## Checklist de evidencias (para no olvidar ninguna)

- [ ] VulnerableApp generando registros (terminal `dotnet run`)
- [ ] Archivos de la carpeta `Logs`
- [ ] Seq mostrando los eventos
- [ ] Grafana con Loki configurado ("successfully connected")
- [ ] Consulta filtrada por labels (`category="audit"`) en Grafana Explore
- [ ] `docker ps` con los 3 contenedores en ejecución
- [ ] `docker-compose.yml` actualizado (ya está en esta carpeta)
- [ ] Archivo de configuración de Promtail (ya está en esta carpeta)
