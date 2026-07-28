# 09 · Onboarding y Offboarding de Usuarios (para el Administrador)

> El sistema no tiene Active Directory/LDAP (ver `00-vision-alcance.md`) —
> cada cuenta se crea desde la UI de Gestión de Usuarios (HU-17, ver
> `docs/epic-04-gestion-catalogos.md`), disponible en `/usuarios` para el
> rol Admin.

## Roles del sistema

| Rol | Puede | No puede |
|---|---|---|
| `Analista` | Cargar/editar Casos e Informes propios, consultar catálogos, buscar | Dar de alta catálogos ni usuarios, ver el tablero de Analítica |
| `Supervisor` | Todo lo de Analista + ver tablero de Analítica y exportar CSV | Dar de alta catálogos ni usuarios |
| `Admin` | Todo lo anterior + gestión de catálogos (Dependencia, Cámara, Barrio, Localidad, Centro de Control) y gestión de usuarios | — |

Ver `src/IGE.Informes.Application/Common/Security/Roles.cs` para la lista
autoritativa — este documento debe actualizarse si se agrega un rol nuevo.

## Alta de un usuario nuevo (onboarding)

**No hay autoregistro** (no existe `Register.razor` ni un endpoint de alta
pública) — es intencional, dado que el sistema maneja datos de
investigaciones en curso y cada cuenta debe originarse en una decisión
explícita del Administrador.

1. Loguearse como Admin y entrar a `Configuración > Usuarios` (`/usuarios`).
2. Click en "Nuevo Usuario".
3. Completar nombre completo, email institucional, una contraseña temporal
   (12+ caracteres — mismo mínimo que exige `PasswordOptions.RequiredLength`)
   y el rol correspondiente.
4. Confirmar el alta.
5. Comunicar la contraseña temporal por un canal **distinto** al que se usó
   para avisarle a la persona que ya tiene cuenta (nunca por el mismo email
   que la identifica).

### Checklist de alta

- [ ] Confirmar el rol correcto (`Analista`/`Supervisor`/`Admin`) antes de
      crear la cuenta.
- [ ] Usar el email institucional real de la persona, no uno genérico.
- [ ] Comunicar la contraseña temporal por un canal separado.
- [ ] Recomendar (no es obligatorio hoy, ver deuda de 2FA abajo) activar el
      2FA desde `Cuenta > Administrar > Autenticación de dos factores` en
      el primer login.

## Baja de un usuario (offboarding)

**No se puede borrar una cuenta desde la UI** (a propósito): el `AuditLog`
referencia `UsuarioId` como FK — si la persona alguna vez cargó un Caso,
firmó un Informe, o quedó registrada en un acceso, borrar el usuario
rompería esas referencias o dejaría huecos en la trazabilidad de auditoría.
En su lugar:

1. Entrar a `Configuración > Usuarios` (`/usuarios`).
2. Abrir la ficha del usuario a dar de baja.
3. Click en "Bloquear" (sección "Bloquear acceso").

Esto reutiliza el mismo mecanismo de lockout de fuerza bruta
(`Identity.Lockout`, ver `docs/06-seguridad-amenazas.md`) para bloqueo
permanente — la cuenta no puede volver a loguearse hasta que un Admin la
desbloquee explícitamente desde la misma ficha.

### Checklist de baja

- [ ] Confirmar con el Administrador/RRHH institucional que la baja es
      efectiva (último día, no una licencia temporal — para licencias,
      considerar si corresponde bloqueo o simplemente esperar).
- [ ] Bloquear la cuenta desde `/usuarios/{id}` — no hay forma de borrarla,
      y no debería hacerse aunque la hubiera.
- [ ] Si la persona tenía una sesión activa, se corta sola: bloquear invalida
      el `SecurityStamp` y `PersistingRevalidatingAuthenticationStateProvider`
      cierra el circuito de Blazor Server en su próximo ciclo de
      revalidación — hasta 30 minutos de latencia (no es corte instantáneo;
      si RRHH necesita corte inmediato, ver deuda abajo).

## Cambiar el rol de un usuario existente

Desde `/usuarios/{id}`, sección "Cambiar rol": seleccionar el nuevo rol y
confirmar. Un usuario tiene un único rol a la vez en este sistema (el
Handler remueve el rol anterior antes de asignar el nuevo, aunque Identity
técnicamente permite roles múltiples — no se prueba ese escenario acá).

## Restricciones de auto-edición

Un Admin **no puede** cambiarse su propio rol ni bloquearse a sí mismo
desde `/usuarios` — la ficha de la propia cuenta no ofrece esas acciones
(protección tanto en la UI como en el Handler, ver
`CambiarRolUsuarioCommandHandler`/`BloquearUsuarioCommandHandler`). Esto
evita que el único Admin del sistema quede sin poder administrar usuarios
por error. Si hace falta cambiar el rol/bloquear a un Admin, debe hacerlo
otro Admin distinto.

## Camino de emergencia (solo si la UI no está disponible)

Si el servicio `web` está caído y hace falta gestionar usuarios de todos
modos (ej. para poder loguearse y diagnosticar el problema), la única vía
es un script de consola contra `UserManager<ApplicationUser>` — igual
patrón que `IdentitySeeder.cs` (`src/IGE.Informes.Infrastructure/Identity/`)
o `UserManagementService.cs`, corrido desde un contenedor temporal conectado
a la red de Docker (ver `07-plan-despliegue.md`, sección de migración). No
insertar filas en `AspNetUsers` a mano con un hash de contraseña inventado
— el hasher es Argon2id, no hay forma de generarlo fuera de la app.

## Deuda pendiente registrada

- La invalidación de sesión al bloquear (o cambiar el rol de) un usuario
  tiene hasta 30 minutos de latencia (intervalo de
  `PersistingRevalidatingAuthenticationStateProvider`) — no es corte
  instantáneo. Si en el futuro se necesita corte inmediato, evaluar bajar
  el intervalo o un mecanismo de notificación activa a los circuitos
  abiertos (no hay infraestructura de SignalR custom hoy para eso).
