using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using IGE.Informes.Application.Common.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IGE.Informes.Infrastructure.Identity;

/// <summary>
/// Crea usuarios de prueba con datos random (nombre, email, contraseña, rol)
/// para poder probar listados/filtros/tablero de Analítica con datos variados
/// sin tener que dar de alta cuentas a mano una por una desde /usuarios.
///
/// Solo corre si ASPNETCORE_ENVIRONMENT=Development Y la variable de entorno
/// IGE_SEED_TEST_USERS=true está presente (doble candado para que nunca se
/// dispare sin querer contra una base de producción). Es idempotente: si ya
/// hay usuarios con el prefijo de email de prueba, no vuelve a crearlos.
///
/// Las credenciales generadas se escriben en un archivo gitignoreado (ver
/// docs/10-usuarios-de-prueba.md) porque es la única forma de que el
/// Administrador las tenga después — Argon2id no permite recuperarlas, y no
/// se loguean por Serilog (regla de CLAUDE.md: nunca contraseñas en texto
/// plano en logs). El directorio de salida es IGE_SEED_TEST_USERS_OUTPUT_DIR
/// (dentro del contenedor no hay checkout del repo, así que en Docker debe
/// apuntar a un volumen montado — ver docker-compose.override.local.yml).
/// </summary>
/// <remarks>Ver <see cref="IdentitySeeder"/> para el seeder del Admin inicial.</remarks>
public static class TestDataSeeder
{
    private const string PrefijoEmail = "prueba.";
    private const string DominioEmail = "@ige.local";

    private static readonly string[] Nombres =
    [
        "Lucía", "Martín", "Sofía", "Nicolás", "Valentina", "Tomás",
        "Camila", "Agustín", "Julieta", "Federico", "Micaela", "Ignacio",
    ];

    private static readonly string[] Apellidos =
    [
        "Fernández", "Gómez", "Rodríguez", "Pérez", "Sosa", "Romero",
        "Acosta", "Díaz", "Molina", "Ibáñez", "Suárez", "Godoy",
    ];

    public static async Task SeedAsync(IServiceProvider services, IHostEnvironment environment)
    {
        if (!environment.IsDevelopment())
        {
            return;
        }

        if (!bool.TryParse(Environment.GetEnvironmentVariable("IGE_SEED_TEST_USERS"), out var habilitado) || !habilitado)
        {
            return;
        }

        using var scope = services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(TestDataSeeder).FullName!);

        if (userManager.Users.Any(u => u.Email != null && u.Email.StartsWith(PrefijoEmail)))
        {
            logger.LogInformation("TestDataSeeder: ya existen usuarios de prueba, no se vuelve a sembrar.");
            return;
        }

        var cantidad = int.TryParse(Environment.GetEnvironmentVariable("IGE_SEED_TEST_USERS_COUNT"), out var n) && n > 0
            ? n
            : 10;

        var distribucionRoles = ArmarDistribucionRoles(cantidad);
        var generados = new List<UsuarioGenerado>(cantidad);
        var usados = new HashSet<string>();

        for (var i = 0; i < cantidad; i++)
        {
            var (nombreCompleto, email) = GenerarIdentidadUnica(usados);
            var password = GenerarPassword();
            var rol = distribucionRoles[i];

            var usuario = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                NombreCompleto = nombreCompleto,
            };

            var resultado = await userManager.CreateAsync(usuario, password);
            if (!resultado.Succeeded)
            {
                logger.LogWarning(
                    "TestDataSeeder: no se pudo crear el usuario de prueba {Email}: {Errores}",
                    email,
                    string.Join("; ", resultado.Errors.Select(e => e.Description)));
                continue;
            }

            var resultadoRol = await userManager.AddToRoleAsync(usuario, rol);
            if (!resultadoRol.Succeeded)
            {
                // Sin rol la cuenta no pasa ninguna policy de autorización (fail-closed),
                // pero dejarla a medias significa una credencial real sin registro en el
                // JSON generado — mejor borrarla y que el conteo final sea menor a pedir.
                await userManager.DeleteAsync(usuario);
                logger.LogWarning(
                    "TestDataSeeder: usuario de prueba {Email} descartado, no se pudo asignar el rol {Rol}: {Errores}",
                    email,
                    rol,
                    string.Join("; ", resultadoRol.Errors.Select(e => e.Description)));
                continue;
            }

            generados.Add(new UsuarioGenerado(usuario.Id, nombreCompleto, email, rol, password));
        }

        try
        {
            var destino = await EscribirRegistroAsync(generados);
            logger.LogInformation(
                "TestDataSeeder: {Cantidad} usuarios de prueba creados. Credenciales en {Destino}.",
                generados.Count,
                destino);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // No dejar que un filesystem read-only o sin permisos tumbe el arranque de
            // la app — las cuentas ya se crearon, solo se pierde el registro en disco.
            logger.LogWarning(
                ex,
                "TestDataSeeder: {Cantidad} usuarios de prueba creados, pero no se pudo escribir el archivo de credenciales.",
                generados.Count);
        }
    }

    private static string[] ArmarDistribucionRoles(int cantidad)
    {
        var roles = new string[cantidad];
        var cortesAdmin = Math.Max(1, (int)Math.Round(cantidad * 0.1));
        var cortesSupervisor = Math.Max(1, (int)Math.Round(cantidad * 0.3));

        for (var i = 0; i < cantidad; i++)
        {
            roles[i] = i < cortesAdmin
                ? Roles.Admin
                : i < cortesAdmin + cortesSupervisor
                    ? Roles.Supervisor
                    : Roles.Analista;
        }

        Random.Shared.Shuffle(roles);
        return roles;
    }

    private static (string NombreCompleto, string Email) GenerarIdentidadUnica(HashSet<string> usados)
    {
        string nombreCompleto;
        string email;
        do
        {
            var nombre = Nombres[Random.Shared.Next(Nombres.Length)];
            var apellido = Apellidos[Random.Shared.Next(Apellidos.Length)];
            nombreCompleto = $"{nombre} {apellido}";
            var sufijo = Random.Shared.Next(100, 999);
            email = $"{PrefijoEmail}{Normalizar(nombre)}.{Normalizar(apellido)}{sufijo}{DominioEmail}";
        }
        while (!usados.Add(email));

        return (nombreCompleto, email);
    }

    private static string Normalizar(string valor) =>
        valor
            .Replace("í", "i").Replace("á", "a").Replace("é", "e").Replace("ó", "o").Replace("ú", "u")
            .Replace("ñ", "n")
            .ToLowerInvariant();

    private static string GenerarPassword()
    {
        const string alfabeto = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789!@#$%";
        Span<char> caracteres = stackalloc char[16];
        for (var i = 0; i < caracteres.Length; i++)
        {
            caracteres[i] = alfabeto[RandomNumberGenerator.GetInt32(alfabeto.Length)];
        }

        return new string(caracteres);
    }

    private static async Task<string?> EscribirRegistroAsync(IReadOnlyCollection<UsuarioGenerado> generados)
    {
        if (generados.Count == 0)
        {
            return null;
        }

        var directorioSalida = Environment.GetEnvironmentVariable("IGE_SEED_TEST_USERS_OUTPUT_DIR")
            ?? Path.Combine(AppContext.BaseDirectory, "generated");
        Directory.CreateDirectory(directorioSalida);
        var destino = Path.Combine(directorioSalida, "usuarios-prueba.generated.json");

        var registro = new
        {
            generadoUtc = DateTime.UtcNow,
            advertencia = "Solo para entorno de Development. No usar estas cuentas en producción.",
            usuarios = generados.Select(u => new
            {
                u.Id,
                u.NombreCompleto,
                u.Email,
                u.Rol,
                u.Password,
            }),
        };

        var json = JsonSerializer.Serialize(registro, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(destino, json, Encoding.UTF8);
        return destino;
    }

    private sealed record UsuarioGenerado(Guid Id, string NombreCompleto, string Email, string Rol, string Password);
}
