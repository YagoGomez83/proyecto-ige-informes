# 09 · Onboarding y Offboarding de Usuarios (para el Administrador)

> **Contexto importante**: el sistema no tiene Active Directory/LDAP (ver
> `00-vision-alcance.md`) y **todavía no existe una pantalla en la UI para
> crear, editar o dar de baja usuarios** — es una deuda de la Fase 5 que
> queda registrada explícitamente en `08-plan-implementacion.md`. Hasta que
> esa UI exista, el alta y baja de usuarios se hace directamente contra la
> base de datos, siguiendo los pasos de este documento. **Siempre en una
> ventana controlada, nunca en caliente sin haber avisado al equipo.**

## Roles del sistema

| Rol | Puede | No puede |
|---|---|---|
| `Analista` | Cargar/editar Casos e Informes propios, consultar catálogos, buscar | Dar de alta catálogos (Dependencia/Cámara/Barrio/Localidad/CCC), ver el tablero de Analítica |
| `Supervisor` | Todo lo de Analista + ver tablero de Analítica y exportar CSV | Dar de alta catálogos |
| `Admin` | Todo lo anterior + gestión de catálogos (Dependencia, Cámara, Barrio, Localidad, Centro de Control), migraciones | — |

Ver `src/IGE.Informes.Application/Common/Security/Roles.cs` para la lista
autoritativa — este documento debe actualizarse si se agrega un rol nuevo.

## Alta de un usuario nuevo (onboarding)

**No hay autoregistro** (no existe `Register.razor` ni un endpoint de alta
pública) — es intencional, dado que el sistema maneja datos de
investigaciones en curso y cada cuenta debe originarse en una decisión
explícita del Administrador.

### Opción A — Consola de administración (`dotnet run` puntual)

La forma más segura hoy: escribir un pequeño script one-off (no versionado,
se descarta después de usarlo) que use `UserManager<ApplicationUser>` igual
que `IdentitySeeder.cs`, corriéndolo una vez contra el entorno real. Ejemplo
de lo que debería hacer (no un script hecho, sino el esqueleto a adaptar):

```csharp
var user = new ApplicationUser
{
    UserName = "nombre.apellido@institucion.gob",
    Email = "nombre.apellido@institucion.gob",
    EmailConfirmed = true,
    NombreCompleto = "Nombre Apellido",
};

var result = await userManager.CreateAsync(user, "<contraseña temporal, 12+ caracteres>");
if (result.Succeeded)
{
    await userManager.AddToRoleAsync(user, Roles.Analista); // o Supervisor/Admin
}
```

Correrlo dentro del contenedor `web` (mismo `AppDbContext`/DI que la app) o
desde un contenedor temporal conectado a la red de Docker, igual que se
hace hoy con `IGE.Informes.DataMigration` (ver `07-plan-despliegue.md`).

### Opción B — Directo contra Postgres (solo si la Opción A no es viable)

**Riesgoso**: el hash de contraseña usa Argon2id (`Argon2PasswordHasher`),
no es trivial de generar a mano fuera de la app — no insertar una fila de
`AspNetUsers` con una contraseña "provisoria" en texto plano ni con un hash
inventado. Si de verdad hace falta este camino, generar el hash con el
mismo `IPasswordHasher<ApplicationUser>` que usa la app (vía un script de
consola como en la Opción A) y solo después escribir la fila.

### Checklist de alta

- [ ] Confirmar el rol correcto (`Analista`/`Supervisor`/`Admin`) antes de
      crear la cuenta — no es trivial de cambiar después sin volver a tocar
      la base (ver sección "Cambiar el rol de un usuario existente").
- [ ] Usar el email institucional real de la persona, no uno genérico.
- [ ] Contraseña temporal de 12+ caracteres (mínimo exigido por
      `PasswordOptions.RequiredLength`, ver `DependencyInjection.cs`),
      comunicada por un canal separado del que se usa para el resto de la
      comunicación de la cuenta (nunca por el mismo email que la identifica).
- [ ] Recomendar (no es obligatorio hoy, ver deuda de 2FA abajo) activar el
      2FA desde `Cuenta > Administrar > Autenticación de dos factores` en
      el primer login.
- [ ] Registrar el alta en la bitácora interna del Administrador (fuera del
      sistema — no hay una pantalla de "usuarios" donde quede este historial
      todavía).

## Baja de un usuario (offboarding)

**No borrar la cuenta** — el `AuditLog` referencia `UsuarioId` como FK; si
la persona alguna vez cargó un Caso, firmó un Informe, o quedó registrada en
un acceso, borrar su usuario de `AspNetUsers` puede romper esas referencias
o, peor, dejar huecos silenciosos en la trazabilidad de auditoría. En su
lugar, bloquear el acceso sin borrar el registro:

```csharp
var user = await userManager.FindByEmailAsync("nombre.apellido@institucion.gob");
await userManager.SetLockoutEnabledAsync(user, true);
await userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
```

Esto reutiliza el mismo mecanismo de lockout de fuerza bruta
(`Identity.Lockout`, ver `docs/06-seguridad-amenazas.md`) para bloqueo
permanente — la cuenta no puede volver a loguearse hasta que un Admin
revierta `LockoutEnd` explícitamente.

### Checklist de baja

- [ ] Confirmar con el Administrador/RRHH institucional que la baja es
      efectiva (último día, no una licencia temporal — para licencias,
      considerar si corresponde bloqueo o simplemente esperar).
- [ ] Bloquear la cuenta (ver arriba) — no borrarla.
- [ ] Si la persona tenía sesiones activas, no hay invalidación inmediata
      de sesión en este sistema (Blazor Server + cookie de Identity) más
      allá de esperar la expiración natural de la cookie o reiniciar el
      contenedor `web` — **deuda pendiente**: evaluar invalidación activa
      de sesión si se vuelve un requisito real (ej. RRHH necesita corte
      inmediato).
- [ ] Registrar la baja en la bitácora interna del Administrador.

## Cambiar el rol de un usuario existente

```csharp
var user = await userManager.FindByEmailAsync("nombre.apellido@institucion.gob");
await userManager.RemoveFromRoleAsync(user, Roles.Analista);
await userManager.AddToRoleAsync(user, Roles.Supervisor);
```

Un usuario puede tener más de un rol simultáneamente en Identity, pero el
diseño de este sistema asume un rol único por persona (ver las policies de
`[Autorizar]` en `Application`) — no asignar más de un rol a la misma cuenta
salvo que se audite primero cómo se comporta el pipeline de autorización
ante roles múltiples (no está probado ese escenario).

## Deuda pendiente registrada (Fase 5)

- No existe una pantalla de administración de usuarios en la UI — todo el
  proceso de este documento es manual, contra la base o vía script de
  consola. Si el número de usuarios crece más allá de lo manejable a mano
  (10-30 usuarios según `07-plan-despliegue.md`, así que puede no ser
  urgente), priorizar construir un HU de "Gestión de Usuarios" (alta,
  cambio de rol, bloqueo/desbloqueo) como página Admin-only, análoga a las
  de catálogos (`docs/epic-04-gestion-catalogos.md`).
- 2FA es autoservicio, no obligatorio para ningún rol — ver
  `docs/06-seguridad-amenazas.md`, sección Autenticación.
- No hay invalidación activa de sesión al hacer offboarding — solo bloqueo
  de futuros logins.
