using IGE.Informes.Application.Common.Interfaces;

namespace IGE.Informes.UnitTests.TestDoubles;

/// <summary>
/// Fake manual de <see cref="IAntivirusScanner"/> — por defecto "limpio"
/// (comportamiento feliz, no bloquea los tests que no le interesa este
/// aspecto), configurable para simular una detección de amenaza o una
/// falla de infraestructura (ClamAV caído).
/// </summary>
public sealed class FakeAntivirusScanner : IAntivirusScanner
{
    public bool ResultadoLimpio { get; set; } = true;
    public bool LanzarNoDisponible { get; set; }

    public Task<bool> EstaLimpioAsync(byte[] contenido, CancellationToken cancellationToken = default)
    {
        if (LanzarNoDisponible)
        {
            throw new AntivirusNoDisponibleException("Simulado: ClamAV no disponible.");
        }

        return Task.FromResult(ResultadoLimpio);
    }
}
