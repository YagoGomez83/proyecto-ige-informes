# 10 · Usuarios de prueba (seeder de desarrollo)

> Solo para entorno local/Development. Este mecanismo está deshabilitado por
> defecto y tiene dos candados para que nunca se dispare contra producción
> (ver "Candados de seguridad" abajo).

## Qué hace

`TestDataSeeder` (`src/IGE.Informes.Infrastructure/Identity/TestDataSeeder.cs`)
crea usuarios con nombre, email y contraseña random al arrancar la app web,
usando el mismo `UserManager<ApplicationUser>` que usa la UI real de
`/usuarios` (HU-17) — no inserta filas a mano, así que las cuentas quedan
exactamente igual de válidas que las creadas por un Admin.

Sirve para tener datos variados sin dar de alta cuentas una por una: probar
el listado de `/usuarios`, el filtro por rol, y el tablero de Analítica
(HU-06), que agrupa por Analista.

- Genera **10 usuarios** por defecto (configurable, ver abajo).
- Distribución de roles aproximada: ~60% `Analista`, ~30% `Supervisor`,
  ~10% `Admin` (mínimo 1 de cada uno de estos dos últimos).
- Emails con el prefijo `prueba.` (ej. `prueba.lucia.gomez482@ige.local`)
  para poder identificarlos a simple vista y para que el seeder sepa si
  ya corrió antes (idempotente: si ya existe al menos un usuario con ese
  prefijo, no vuelve a sembrar).
- Contraseñas de 16 caracteres generadas con `RandomNumberGenerator`
  (criptográficamente random, cumplen el mínimo de 12 caracteres de
  `PasswordOptions.RequiredLength`).
- Si no se puede asignar el rol a una cuenta recién creada, esa cuenta se
  borra en el momento (no quedan usuarios "a medias" sin rol ni sin
  registro en el archivo de credenciales).

## Candados de seguridad

El seeder **no corre** a menos que se cumplan las dos condiciones:

1. `ASPNETCORE_ENVIRONMENT=Development` (chequeo de `IHostEnvironment`, no
   de una variable que alguien podría copiar mal a producción).
2. La variable de entorno `IGE_SEED_TEST_USERS=true` está presente.

Esto es intencional: `docs/09-onboarding-offboarding-usuarios.md` deja
explícito que no hay autoregistro en este sistema porque maneja datos de
investigaciones en curso — el seeder no es una excepción a esa regla, es
una herramienta de desarrollo que debe quedar apagada en todo lo demás.
`docker-compose.yml` (el compose base, versionado) define ambas variables
con default seguro (`IGE_SEED_TEST_USERS=false`), así que un despliegue que
no las toque nunca siembra nada.

## Cómo usarlo

### 1. Habilitar en `docker/.env` (gitignoreado, no se commitea)

```
ASPNETCORE_ENVIRONMENT=Development
IGE_SEED_TEST_USERS=true
IGE_SEED_TEST_USERS_COUNT=10   # opcional, default 10
```

### 2. Montar un volumen para poder leer el archivo de credenciales después

El contenedor `web` corre con un usuario no-root (`ige`) y **no tiene el
repo montado** — no existe una ruta de host tipo `docker/` dentro del
contenedor. Por eso el archivo se escribe en `/app/generated` dentro del
contenedor, y hace falta un volumen para poder leerlo desde afuera.
`docker/docker-compose.override.local.yml` (gitignoreado, mismo archivo que
ya expone el puerto 8080 para desarrollo local) ya lo declara:

```yaml
services:
  web:
    volumes:
      - seed_test_users_output:/app/generated

volumes:
  seed_test_users_output:
```

Si se prefiere un bind mount a una carpeta real del host en vez de un
volumen nombrado de Docker, hay que replicar el `chown ige:ige` que hace
`docker/Dockerfile` sobre `/app/generated` — si no, `UnauthorizedAccessException`
al escribir (el usuario del proceso no es el dueño de una carpeta creada
por un bind mount).

### 3. Levantar (o reconstruir) el contenedor `web`

```
docker compose build web
docker compose up -d --force-recreate web
```

### 4. Confirmar que sembró

```
docker compose logs web | grep TestDataSeeder
```

## Dónde quedan las credenciales

El seeder escribe **una sola vez** el detalle completo (id, nombre, email,
rol, contraseña en texto plano) en `/app/generated/usuarios-prueba.generated.json`
dentro del contenedor — que con el volumen de arriba persiste entre
reinicios. Para copiarlo al host y poder abrirlo cómodo:

```
docker compose cp web:/app/generated/usuarios-prueba.generated.json docker/generated/usuarios-prueba.generated.json
```

`docker/generated/` está en `.gitignore` — nunca se commitea. Es la única
copia de las contraseñas: como el hasher es Argon2id
(`Argon2PasswordHasher.cs`), no hay forma de recuperarlas después desde la
base. Si se pierde el archivo, hay que resetear la contraseña de esas
cuentas (no hay UI de reset todavía, ver deuda pendiente en
`docs/09-onboarding-offboarding-usuarios.md`) o volver a sembrar con un
prefijo distinto.

**Por qué no van a Serilog**: la regla de `CLAUDE.md` prohíbe loguear
contraseñas en texto plano en logs — por eso el log de consola solo
confirma cuántos usuarios se crearon y la ruta del archivo, nunca las
contraseñas.

## Para volver a sembrar desde cero

El seeder es idempotente (no crea duplicados mientras exista al menos un
usuario `prueba.*`), así que para regenerar el set completo con datos
nuevos hay que borrar las cuentas de prueba existentes primero — desde
`/usuarios` (bloquearlas no alcanza, hay que eliminarlas a nivel de base
porque no hay borrado por UI, ver offboarding) o con un script puntual
contra `AspNetUsers`/`AspNetUserRoles` filtrando por el prefijo `prueba.`.
No hay un comando de "reset" automático — es deliberado, para no facilitar
borrar usuarios reales por accidente.

## Qué NO hace

- No crea Casos de Análisis, Informes, Vehículos ni Personas de prueba —
  solo cuentas de usuario. Si hace falta un seeder de esas entidades, es
  trabajo aparte (evaluar primero si conviene reusar la migración masiva
  de HU-04/HU-08 con un dataset chico en vez de un seeder nuevo).
- No se ejecuta en el pipeline de CI ni en ningún `docker-compose.yml` por
  defecto — hay que habilitarlo a mano en `docker/.env` local.
