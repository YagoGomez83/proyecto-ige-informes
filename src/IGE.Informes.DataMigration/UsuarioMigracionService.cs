using IGE.Informes.Application.Common.Interfaces;

namespace IGE.Informes.DataMigration;

public sealed class UsuarioMigracionService : ICurrentUserService
{
    // Guid reservado y documentado como "usuario de sistema / migración
    // histórica" — nunca corresponde a una cuenta real de Identity.
    public static readonly Guid UsuarioMigracionId = new("00000000-0000-0000-0000-000000000001");

    public Guid? UsuarioId => UsuarioMigracionId;
    public IReadOnlyCollection<string> Roles => ["Admin"];
}
