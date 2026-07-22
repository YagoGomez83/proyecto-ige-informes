using IGE.Informes.Application.Common.Interfaces;

namespace IGE.Informes.UnitTests.TestDoubles;

public sealed class FakeFileStorage : IFileStorage
{
    public List<string> ArchivosSubidos { get; } = [];

    public Task<string> SubirAsync(string nombreArchivo, Stream contenido, string tipoMime, CancellationToken cancellationToken = default)
    {
        var clave = $"fake/{nombreArchivo}";
        ArchivosSubidos.Add(clave);
        return Task.FromResult(clave);
    }

    public Task<string> ObtenerUrlDescargaAsync(string clave, CancellationToken cancellationToken = default) =>
        Task.FromResult($"https://fake-storage.local/{clave}");
}
