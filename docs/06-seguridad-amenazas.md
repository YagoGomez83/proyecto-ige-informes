# 06 · Seguridad y Modelo de Amenazas

## Clasificación de datos

Este sistema maneja **datos sensibles de investigaciones judiciales/policiales
en curso**: causas, DNI de denunciantes/damnificados/sospechosos, patentes de
vehículos, imágenes de personas y recorridos. Se lo trata con el mismo
estándar de cuidado que un sistema de salud o financiero, aunque no exista
una ley específica citada por el equipo — es la postura por defecto dada la
naturaleza del dato.

| Dato | Clasificación | Implicancia |
|---|---|---|
| DNI, nombre de Persona | Dato personal sensible | Acceso restringido + auditoría de lectura |
| Imágenes de personas/vehículos | Dato personal sensible | Acceso restringido, URLs firmadas con expiración |
| Relato/observaciones de Caso e Informe | Información de investigación en curso | Confidencial, acceso solo a roles autorizados |
| Carátula/pieza sumarial | Dato de expediente | Confidencial |
| Credenciales de usuario | Secreto | Hash Argon2id, nunca en logs |

## Modelo de amenazas (STRIDE) por componente

### 1. Autenticación / gestión de usuarios

| Amenaza | Riesgo | Control |
|---|---|---|
| **S**poofing — suplantación de identidad | Alto (sin AD/LDAP, credenciales propias) | Hash de contraseñas con **Argon2id**; política de contraseña mínima (12+ caracteres); **2FA (TOTP) obligatorio para Supervisor y Admin** — tras un login con contraseña correcta, si el rol es Supervisor/Admin y `TwoFactorEnabled = false`, se lo redirige a configurarlo (`Account/ConfigurarDosFactoresObligatorio`) antes de poder acceder a cualquier otra página; no hay forma de posponerlo ni saltearlo. Para Analista sigue siendo autoservicio opcional (`Manage/EnableAuthenticator`) |
| **S**poofing — fuerza bruta / credential stuffing | Alto | Bloqueo de cuenta tras 5 intentos fallidos (lockout progresivo, `Identity.Lockout`); rate limiting por IP en las rutas `/Account` (10 req/min, `Microsoft.AspNetCore.RateLimiting`, ver `src/IGE.Informes.Web/Program.cs`) — requiere `ForwardedHeaders` configurado para que la IP particionada sea la del cliente real y no la del reverse proxy, ver Fase 5 |
| **E**levation of Privilege | Medio | RBAC estricto por policy (`Analista` / `Supervisor` / `Admin`) validado en el backend en cada Command/Query — nunca confiar en el rol mostrado en el cliente |
| **E**levation of Privilege — circuito de Blazor Server con permisos obsoletos tras bloqueo/cambio de rol | Medio (mitigado) | `BloquearAsync`/`CambiarRolAsync` (`UserManagementService`) invalidan el `SecurityStamp` del usuario; `PersistingRevalidatingAuthenticationStateProvider` lo revalida cada 30 min y cierra el circuito si no coincide — evita que una sesión ya abierta siga operando con el rol/estado viejo indefinidamente. Latencia de hasta 30 min, no es corte instantáneo (ver `docs/09-onboarding-offboarding-usuarios.md`) |
| **R**epudiation | Medio | Toda acción de login/logout queda en `AuditLog` con IP y user-agent |

### 2. Acceso a datos (Informes, Casos, Vehículos, Personas)

| Amenaza | Riesgo | Control |
|---|---|---|
| **I**nformation Disclosure — acceso no autorizado a un Informe/Persona | Alto | Autorización a nivel de Application layer (no solo UI); todo acceso de lectura a `Informe`, `CasoAnalisis`, `Vehiculo`, `Persona` se registra en `AuditLog` (usuario, entidad, timestamp) — es requisito, no opcional |
| **T**ampering — modificación no autorizada de un Informe publicado | Alto | Un `Informe` en estado `Publicado` es inmutable salvo por un flujo explícito de "corrección" que genera nueva versión y conserva el historial (nunca sobreescribe) |
| **T**ampering — race condition entre `PublicarInforme` y otro Command que muta el Informe (ej. `VincularVehiculoInforme`/`VincularPersonaInforme`) | Alto (corregido) | Hallazgo del security-reviewer al cerrar la Fase C: el chequeo de `Estado == Publicado` en el Handler no estaba protegido contra una publicación concurrente entre la lectura y el `SaveChangesAsync` — podía colarse una vinculación sobre un Informe ya `Publicado`. Corregido con token de concurrencia optimista sobre la columna de sistema `xmin` de Postgres (`InformeConfiguration.UseXminAsConcurrencyToken()`) — cualquier `SaveChangesAsync` sobre una fila de `Informe` modificada por otra transacción en el medio lanza `DbUpdateConcurrencyException`, capturada y traducida a un error explícito en los tres Handlers que mutan `Informe` (`PublicarInforme`, `VincularVehiculoInforme`, `VincularPersonaInforme`) |
| **T**ampering — condición de carrera en el índice único de `PersonaVehiculo` | Bajo (riesgo aceptado) | Dos requests concurrentes vinculando el mismo par Persona/Vehículo: el índice único `(PersonaId, VehiculoId)` previene el duplicado a nivel de base, pero el segundo request propaga `DbUpdateException` sin traducir a éxito idempotente (a diferencia del camino no-concurrente, que sí lo detecta por lectura previa). No se agrega manejo específico porque requeriría que `Application` dependa del tipo de excepción de Npgsql (rompe Clean Architecture) para un caso extremo (doble click en la misma ventana de milisegundos) — la UI ya muestra un mensaje de error genérico ante esto, sin corromper datos |
| **T**ampering — posible duplicación de `Alerta` bajo concurrencia | Bajo (riesgo aceptado) | Hallazgo del security-reviewer al cerrar la Fase D: `Alerta` no tiene ningún índice único (a diferencia de `PersonaVehiculo`) — dos vinculaciones concurrentes del mismo Vehículo/Persona desde dos Informes distintos, cada una sin ver todavía el vínculo del otro, podrían generar dos `Alerta` de tipo `CargaHuerfana` en vez de una `CargaHuerfana` + una `Reincidencia`, o Alertas duplicadas. Es una entidad de notificación, no de control de acceso — no hay corrupción de datos de negocio ni bypass de autorización, solo ruido informativo en `/alertas` en una ventana de milisegundos. Se acepta el riesgo sin agregar un índice único (el par Vehiculo/Informe puede repetirse legítimamente entre una Alerta de Reincidencia y una futura corrección, a diferencia de `PersonaVehiculo` que sí modela una relación 1:1 real) |
| **T**ampering — código de identificación repetido entre entidades (`Camara.Codigo`) | Bajo | Decisión de diseño explícita (Fase 7): a diferencia del resto de los identificadores de negocio del proyecto, `Camara.Codigo` **no** es único — el relevamiento real trae códigos repetidos entre cámaras de una misma instalación agrupada (ver `docs/01-glosario-dominio.md`). No representa una amenaza de seguridad (no es un identificador de acceso ni de autorización), solo una particularidad de modelado a tener presente si se agrega lógica nueva que asuma unicidad |
| **R**epudiation — negar haber modificado/consultado algo | Medio | `AuditLog` append-only, sin permisos de borrado ni para Admin desde la aplicación |
| **I**nformation Disclosure — archivos (PDFs/imágenes) accesibles por URL directa | Alto | MinIO con **URLs prefirmadas de corta expiración** (nunca URLs públicas permanentes); bucket privado por defecto |
| **I**nformation Disclosure — vista previa embebida del PDF en `Informes/Detalle.razor` | Medio | La URL prefirmada (5 min de expiración, `MinioOptions.UrlDescargaExpiracionSegundos`) queda visible en el DOM (`src` del `<iframe>`) mientras la página está abierta — quien inspeccione el elemento o copie el link puede acceder al PDF sin sesión de la app durante esa ventana. **Riesgo aceptado explícitamente**: no es un cambio de superficie respecto al mecanismo de descarga ya existente (misma URL, misma expiración), solo la primera vez que se genera automáticamente al abrir la página en vez de bajo un click explícito de "Descargar". Sin control adicional (ej. un solo uso) por ahora — revisar si en el futuro se necesita algo más estricto para Informes con datos especialmente sensibles. **Extiende también a listados que generan N URLs prefirmadas de antemano** (`ListarImagenesVehiculoQueryHandler`, `ListarImagenesPersonaQueryHandler`, y desde HU-04 también `ListarMigracionesPendientesQueryHandler` para el visor de PDF de cada Migración Pendiente en `/informes/migrar/pendientes`) — mismo riesgo aceptado: todas las URLs del listado completo viajan en la respuesta al Admin/Analista ya autorizado, aunque la UI solo renderice el `<iframe>`/`<img>` de un ítem a la vez (hallazgo del security-reviewer al agregar el visor de Migraciones Pendientes: el documento originalmente solo describía el caso de una URL por página) |
| **I**nformation Disclosure — `SugerirCausasQuery` (HU-02, edición de Informe) expone `Causa` sin scoping por Dependencia | Bajo (riesgo aceptado) | Hallazgo del security-reviewer al agregar la sugerencia de Causas existentes por similaridad de carátula (`pg_trgm`/`similarity()`): la Query devuelve candidatas de **todo el sistema**, sin filtrar por la Dependencia del usuario ni por si tiene acceso al Informe dueño de cada Causa — es la primera vez que se puede enumerar `Causa` (dato de expediente, confidencial) por texto libre en vez de verla solo colgada de un Informe ya autorizado. **Riesgo aceptado explícitamente**: ningún otro Query del sistema (`BuscarInformesQuery`, `BuscarCasosQuery`, etc.) filtra por Dependencia hoy — los 3 roles (Analista/Supervisor/Admin) ya ven todo el universo de datos, es el diseño actual. Acotar `SugerirCausasQuery` sería la primera restricción de este tipo, inconsistente con el resto del sistema salvo que se decida cambiar el criterio general |
| **T**ampering — auto-match de `Causa` en migración masiva (HU-04) sin revisión humana | Bajo (riesgo aceptado, mitigación detectiva) | Hallazgo del security-reviewer al extender el matching de Causa por N° de Pieza Sumarial (ya usado y revisado en HU-02) a `MigrarInformesCommandHandler`/`CrearInformeDesdeMigracionPendienteCommandHandler`: a diferencia de la edición manual (el usuario ve la sugerencia y confirma antes de vincular), acá el auto-match vincula sin que ningún humano lo revise en el momento — una colisión accidental de N° de Pieza Sumarial (parser extrayendo mal el dato, coincidiendo por casualidad con una `Causa` real no relacionada) vincularía en silencio al expediente equivocado. Mitigado con `AuditLog("CausaAutoAsignadaMigracion", ...)` en cada auto-vinculación y el reporte `/informes/causas-auto-asignadas` (Admin, `ListarCausasAutoAsignadasQuery`) para revisión posterior. **Riesgo aceptado explícitamente sobre la mitigación en sí**: es detectiva, no preventiva — el Informe queda operable con la Causa auto-asignada desde que se crea, y el reporte no distingue ítems ya revisados de nuevos (sin botón de "marcar revisado", confirmado con el usuario). Aceptable mientras el volumen de migraciones masivas sea manejable a ojo. **Actualizado**: `/informes/migrar/pendientes` ahora también ofrece sugerencias de Causa por similaridad de carátula (mismo mecanismo que `SugerirCausasQuery` de HU-02, ver fila de Information Disclosure más abajo) para el caso en que el auto-match exacto no encuentre nada — a diferencia del auto-match, esta es una elección explícita del usuario en la UI, por lo que `CrearInformeDesdeMigracionPendienteCommandHandler` **no** genera `AuditLog("CausaAutoAsignadaMigracion", ...)` cuando la Causa vino de `request.CausaId` (sí hubo revisión humana en el momento). El `Guid` de Causa elegido se valida contra la base antes de vincularlo (`EntidadNoEncontradaException` si no existe), evitando que el cliente fuerce una vinculación a un Id arbitrario. Las sugerencias en esta pantalla se cargan bajo demanda por fila (botón "Buscar Causas parecidas"), no todas juntas al abrir la página — con el volumen real de Migraciones Pendientes (~100+), disparar una consulta `similarity()` de Postgres por cada una al cargar sería una ráfaga de queries sin ningún tope |
| **T**ampering — race condition en `EliminarDependenciaCommand` (extensión HU-11) entre el chequeo de uso y el borrado | Bajo (riesgo aceptado) | Hallazgo del security-reviewer al agregar el borrado de `Dependencia`: el Handler valida con `AnyAsync` que ninguna entidad (`Informe`, `Camara`, `CasoAnalisis`, `MigracionPendiente`, otra `Dependencia` vía `UnidadRegionalId`) la referencie, pero no hay transacción explícita ni token de concurrencia entre esas lecturas y el `Remove`+`SaveChangesAsync` — a diferencia de `Informe` (que sí tiene `UseXminAsConcurrencyToken()`), `Dependencia` no tiene columna de concurrencia. Una creación/asignación concurrente en la ventana de milisegundos entre el chequeo y el borrado dejaría esa referencia nueva apuntando a un `Guid` inexistente. **Riesgo aceptado explícitamente**: es una operación de Admin sobre un catálogo de baja frecuencia de escritura concurrente (a diferencia de `Informe`, que varios Analistas editan a la vez); de ocurrir, el síntoma es visible (referencia rota) y detectable, no una corrupción silenciosa de datos de investigación. Además, ninguna de las cuatro entidades que referencian `Dependencia` tiene FK real a nivel de base salvo `Camara` (`CamaraConfiguration.HasForeignKey`) — `CasoAnalisis`, `Informe` y `MigracionPendiente` solo tienen índice, no constraint — por lo que el chequeo de `Application` ya es, en la práctica, la única red de seguridad tanto para el caso secuencial como para el concurrente. `ListarInformesPaginadoQueryHandler` usa `left join` (`DefaultIfEmpty()`) contra `Dependencia`, no `inner join`, precisamente para que un `Informe` con `DependenciaDestinoId` huérfano siga apareciendo en el listado (mostrando "Dependencia no encontrada") en vez de desaparecer silenciosamente — decisión tomada al agregar ese listado, ver fila de Information Disclosure más abajo |
| **I**nformation Disclosure — Carátula de `Causa` visible en el listado general pasivo de Informes (`/informes`, extensión de HU-01) | Bajo (riesgo aceptado) | Hallazgo del security-reviewer al agregar Causa/Dependencia como columnas de `ListarInformesPaginadoQuery`: es la primera vez que la Carátula de una `Causa` (dato de expediente, "Confidencial" según la tabla de clasificación de la sección 1) aparece en un listado **pasivo** — sin que el usuario dispare una búsqueda o acción explícita, alcanza con entrar a `/informes` — para los 3 roles (Analista/Supervisor/Admin). No es exactamente el mismo caso que `SugerirCausasQuery` (esa es una búsqueda activa por similaridad de texto, iniciada por el usuario) ni que `ListarMigracionesPendientesQuery` (ese listado sí muestra Carátula pero está restringido a `Roles.Admin`). **Riesgo aceptado explícitamente extendiendo el mismo criterio ya sentado para `SugerirCausasQuery`**: los 3 roles ya ven el universo completo de `Informe`/`CasoAnalisis`/`Vehiculo`/`Persona` hoy, sin scoping por Dependencia — no filtrar `Causa` en este listado es consistente con esa postura general, no una excepción nueva. Revisar si en el futuro se introduce scoping por Dependencia a nivel de sistema, este listado debería alinearse igual que el resto |
| **T**ampering — race condition en la validación de Dominio/DNI duplicado al registrar Vehículo/Persona (extensión HU-02, atajo de Editar Informe) | Bajo (riesgo aceptado) | Hallazgo del security-reviewer al agregar el chequeo `AnyAsync` por `Dominio`/`Dni` en `RegistrarVehiculoCommandHandler`/`RegistrarPersonaCommandHandler`: mismo patrón no transaccional ya aceptado para `CrearDependenciaCommandHandler`, pero a diferencia de `Dependencia` (catálogo de Admin, baja frecuencia de escritura), `Vehiculo`/`Persona` son datos de operación diaria editados por varios Analistas a la vez — la ventana de carrera es más real. Además, ni `VehiculoConfiguration` ni `PersonaConfiguration` tienen `HasIndex(...).IsUnique()` sobre `Dominio`/`Dni` (a diferencia de `PersonaVehiculo`, que sí tiene índice único como red de seguridad final) — dos altas concurrentes con el mismo Dominio/DNI podrían quedar ambas persistidas, no solo fallar como en el caso de `PersonaVehiculo`. **Riesgo aceptado explícitamente**: el caso realista (dos Analistas cargando el mismo Vehículo/Persona real en la misma ventana de milisegundos, con el mismo Dominio/DNI tipeado idéntico) es infrecuente, y el síntoma (dos fichas separadas del mismo Vehículo/Persona) es detectable y corregible manualmente — no hay bypass de autorización ni corrupción de datos de investigación, solo un duplicado de catálogo. Agregar `IsUnique()` requeriría decidir una política de resolución de conflicto (¿cuál gana?) fuera del alcance de este cambio |

### 3. Ingesta de PDFs (upload + extracción)

| Amenaza | Riesgo | Control |
|---|---|---|
| **T**ampering — subida de archivo malicioso disfrazado de PDF | Medio-Alto | Validación de tipo MIME real (no solo extensión), tamaño máximo, escaneo con antivirus (ClamAV, protocolo INSTREAM por TCP) antes de persistir en MinIO — implementado en `ConfirmarCargaInformeCommandHandler` (HU-01), único punto que sube archivos a MinIO. **Fail-closed**: si ClamAV no responde (`AntivirusNoDisponibleException`) la carga se rechaza sin persistir nada, en vez de aceptar el archivo sin escanear |
| Denial of Service — PDF corrupto o "PDF bomb" que cuelga el parser | Medio | **Mitigación parcial**: timeout de 30 s por archivo (`PdfParserTimeoutHelper`, `Task.Run` + `CancelAfter`) en `ParsearPdfInformeQueryHandler` (HU-01) y `MigrarInformesCommandHandler` (HU-04) — corta el bloqueo percibido por el usuario/circuito y evita que un archivo colgado tumbe el resto de un lote. **Límite real, no un worker aislado**: el hilo de PdfPig no coopera con cancelación, así que tras el timeout el hilo queda huérfano en el thread pool hasta que el parseo real termine (o nunca, en el peor caso) — no hay proceso/contenedor separado que se pueda matar. Migración masiva además acotada a 40 archivos por corrida (`MigrarInformesCommandValidator.CantidadMaximaArchivos`) por el riesgo de memoria (ver sección de límites de recursos) |
| Injection — contenido del PDF usado sin sanitizar en la UI | Medio | Todo texto extraído se trata como dato, nunca se interpola en HTML sin encode (Blazor ya escapa por defecto — evitar `MarkupString` con contenido no confiable) |

**Incidente cerrado (2026-07-31)**: un bug en `InformePdfParser.ExtraerRelato` (el patrón de corte de fin de relato no reconocía la variante real "Imagen N" sin el símbolo "N°") hizo que el campo `Relato` de 14 Informes migrados (todos en `Borrador`, sin PDF original guardado) quedara con el documento completo en vez de solo el párrafo narrativo — incluyendo, en algunos casos, datos personales de un **tercero no vinculado al Informe** (titular real de un vehículo con DNI y dirección, extraído del bloque de "Registro de la Propiedad del Automotor" que el PDF incluye como referencia técnica). Corregido: patrón de corte ampliado + filtro de ruido de paginación ("Página X de Y") en `InformePdfParser`, y los 14 Informes ya existentes limpiados con el subcomando `limpiar-relatos` de `IGE.Informes.DataMigration` (dry-run por defecto, solo toca el campo `Relato`, nunca un Informe `Publicado`). **Exposición evaluada como baja**: mientras el dato estuvo persistido, era legible por cualquier usuario con rol Analista/Supervisor/Admin autorizado a `BuscarInformesQuery`/`ObtenerInformePorIdQuery` (no hubo acceso externo ni de roles no autorizados) — se confirmó además que el dato sensible quedaba fuera de los primeros 200 caracteres que muestra el listado de resultados de búsqueda (el párrafo narrativo legítimo siempre precede a los datos técnicos en la plantilla), por lo que no se mostraba en resultados de búsqueda, solo en el Detalle completo del Informe. Ver memoria del proyecto para el detalle completo de la investigación y el script de limpieza.

### 4. Imágenes de Vehículo/Persona (upload) — HU-08/HU-09

| Amenaza | Riesgo | Control |
|---|---|---|
| **T**ampering — subida de archivo malicioso disfrazado de imagen | Medio | Mismo patrón que la ingesta de PDFs (sección 3): whitelist de `TipoMime` (JPEG/PNG/WebP), tamaño máximo (10MB), escaneo con ClamAV antes de persistir en MinIO, fail-closed. Además, el `Content-Type` declarado por el navegador se **confirma contra los magic bytes reales** del contenido (`FormatoImagenHelper.CoincideConTipoDeclarado`, en `AgregarImagenVehiculoCommandValidator`) — no se confía únicamente en metadata controlada por el cliente. Hallazgo del security-reviewer al cerrar la Fase A: la primera versión solo validaba el `TipoMime` declarado, sin verificar el contenido — corregido antes de cerrar la fase |
| **I**nformation Disclosure — imagen accesible por URL directa | Alto | Mismo mecanismo que PDFs/Evidencia: `ListarImagenesVehiculoQuery` nunca expone `ImagenPath` crudo, solo URLs prefirmadas de corta expiración resueltas en el Handler |
| **T**ampering — eliminación de imagen sin borrar el archivo físico | Bajo | `QuitarImagenVehiculoCommand` (restringido a Supervisor/Admin) llama `IFileStorage.EliminarAsync` antes de borrar el registro — no queda un archivo huérfano en MinIO |
| Path traversal en la clave de almacenamiento | Bajo (no explotable) | La clave de MinIO se genera siempre server-side (`{Guid.NewGuid():N}/{nombreArchivo}`) y el usuario nunca la referencia directamente — el Command de baja recibe `VehiculoImagenId` (Guid), no la clave. El SDK de MinIO trata el object key como string opaco (sin resolución de rutas tipo filesystem), por lo que no hay vector de traversal real aunque `nombreArchivo` no esté sanitizado |

### 5. Infraestructura on-premise

| Amenaza | Riesgo | Control |
|---|---|---|
| **T**ampering — acceso físico/de red no autorizado al servidor | Alto (depende de la institución) | Servidor en red interna, sin exposición directa a Internet; acceso solo vía VPN/LAN institucional; firewall restringiendo puertos (solo 443 hacia la app, resto solo localhost/red interna) |
| **I**nformation Disclosure — tráfico en claro dentro de la LAN | Medio | TLS interno (certificado propio o de la institución) incluso dentro de la LAN — no asumir que "es red interna, no hace falta" |
| Secrets en texto plano (connection strings, claves) | Alto | Variables de entorno vía Docker secrets o `.env` **fuera del control de versiones**; nunca secretos hardcodeados en `appsettings.json` commiteado |
| Falta de backups / pérdida de datos | Alto | Ver `07-plan-despliegue.md` — backups automatizados de PostgreSQL y del bucket MinIO |

## 2FA obligatorio para Supervisor y Admin

```gherkin
Característica: 2FA obligatorio para roles Supervisor y Admin

  Escenario: Admin sin 2FA activado intenta loguearse
    Dado que soy un usuario con rol "Admin" y no tengo 2FA activado
    Cuando ingreso mi email y contraseña correctos
    Entonces se me redirige a configurar 2FA antes de poder acceder a
    cualquier otra página del sistema

  Escenario: Supervisor sin 2FA activado intenta loguearse
    Dado que soy un usuario con rol "Supervisor" y no tengo 2FA activado
    Cuando ingreso mi email y contraseña correctos
    Entonces se me redirige a configurar 2FA antes de poder acceder a
    cualquier otra página del sistema

  Escenario: Analista sin 2FA activado intenta loguearse
    Dado que soy un usuario con rol "Analista" y no tengo 2FA activado
    Cuando ingreso mi email y contraseña correctos
    Entonces accedo normalmente al sistema, sin que se me exija 2FA

  Escenario: Admin completa la configuración obligatoria de 2FA
    Dado que fui redirigido a configurar 2FA de forma obligatoria
    Cuando escaneo el código QR e ingreso el código de verificación válido
    Entonces mi 2FA queda activado y accedo a la página que quería
    visitar originalmente

  Escenario: Admin con 2FA ya activado hace login normal
    Dado que soy Admin y ya tengo 2FA activado
    Cuando ingreso mi email y contraseña correctos
    Entonces se me pide el código de mi aplicación de autenticación
    (flujo actual, sin cambios)

  Escenario: Usuario promovido a Supervisor debe configurar 2FA en su
  próximo login
    Dado que un Administrador me cambia el rol de "Analista" a "Supervisor"
    Y yo no tengo 2FA activado
    Cuando hago login la próxima vez
    Entonces se me exige configurar 2FA antes de continuar
```

### Notas de implementación

- El enforcement corre en `Login.razor` después de `PasswordSignInAsync`
  exitoso, consultando el rol actual del usuario vía `UserManager` — no
  hace falta ningún cambio en `CambiarRolUsuarioCommandHandler`, el
  siguiente login ya lo cubre.
- La página de configuración obligatoria reutiliza el mismo mecanismo de
  `Manage/EnableAuthenticator` (QR + código TOTP), pero en una ruta propia
  sin acceso al resto del sistema hasta completarla.
- No hay período de gracia: la cuenta queda autenticada (cookie creada)
  pero **no puede navegar a ninguna otra página** hasta terminar el setup
  — decisión explícita, más simple que trackear una fecha límite.

## Checklist OWASP Top 10 (mapeo rápido)

| OWASP | Aplica | Mitigación |
|---|---|---|
| A01 Broken Access Control | Sí | RBAC server-side en Application layer, nunca solo en UI |
| A02 Cryptographic Failures | Sí | Argon2id para passwords, TLS en tránsito, MinIO con cifrado en reposo |
| A03 Injection | Sí | EF Core con queries parametrizadas (nunca SQL concatenado); validación de entrada con FluentValidation |
| A04 Insecure Design | Sí | Threat model documentado (este archivo), revisado en cada nueva feature sensible |
| A05 Security Misconfiguration | Sí | Contenedores con usuario no-root, imágenes base mínimas, headers de seguridad: HSTS/X-Frame-Options/etc. en el reverse proxy, Content-Security-Policy armada por la app (`CspMiddleware.cs`, necesita el hash SRI del ImportMap de Blazor) |
| A07 Identification and Authentication Failures | Sí | Ver sección 1 |
| A08 Software and Data Integrity Failures | Sí | `.github/workflows/ci.yml` corre `dotnet list package --vulnerable --include-transitive` (JSON parseado con `jq`) y falla el build si aparece algún paquete NuGet con vulnerabilidad conocida, más un escaneo de la imagen Docker final con Trivy (severidad HIGH/CRITICAL, `ignore-unfixed`) — falla el build ante cualquier hallazgo |
| A09 Security Logging and Monitoring Failures | Sí | `AuditLog` + logs estructurados (Serilog) centralizados; alertas ante múltiples intentos de login fallidos |

## Gestión de secretos y CI/CD

- Ningún secreto en el repositorio Git (usar `.gitignore` para `.env`,
  `appsettings.Production.json`).
- Pipeline de CI ejecuta: build, tests, análisis estático (opcional
  SonarQube/Roslyn analyzers), escaneo de dependencias vulnerables y
  escaneo de la imagen Docker antes de permitir el deploy.
- El deploy a producción (servidor on-premise) es manual o vía script
  controlado por el Administrador — no hay pipeline de deploy automático
  hacia el servidor institucional sin aprobación humana, dado que es
  infraestructura crítica de un organismo público.

## Retención y borrado de datos

- Las grabaciones/videos originales de las cámaras **no** se almacenan en
  este sistema (siguen su propio ciclo de 30 días en los sistemas de
  videovigilancia, según se documenta en el pie de cada Informe). El sistema
  solo guarda las **imágenes fijas** ya extraídas como Evidencia.
- No hay por ahora un requisito de borrado automático de Informes/Casos
  (son antecedentes de investigaciones que pueden reabrirse) — se conservan
  indefinidamente salvo instrucción institucional en contrario.
