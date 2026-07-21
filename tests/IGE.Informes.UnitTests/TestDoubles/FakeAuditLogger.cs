using IGE.Informes.Application.Common.Interfaces;

namespace IGE.Informes.UnitTests.TestDoubles;

public sealed class FakeAuditLogger : IAuditLogger
{
    public List<(string Accion, string Entidad, Guid? EntidadId)> Registros { get; } = [];

    public Task RegistrarAccesoAsync(string accion, string entidad, Guid? entidadId, CancellationToken cancellationToken = default)
    {
        Registros.Add((accion, entidad, entidadId));
        return Task.CompletedTask;
    }
}
