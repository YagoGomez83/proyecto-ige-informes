using IGE.Informes.Application.Common.Security;
using MediatR;

namespace IGE.Informes.Application.Informes.Commands.MigrarInformes;

public sealed record PdfMigrarDto(byte[] Contenido, string NombreArchivo);

public enum ResultadoMigracionArchivo
{
    Exitoso,
    ConAdvertencia,
    Fallido,
}

public sealed record MigracionArchivoResultDto(string NombreArchivo, ResultadoMigracionArchivo Resultado, string? Motivo);

public sealed record MigracionLoteResultDto(
    int TotalProcesados,
    int Exitosos,
    int ConAdvertencia,
    int Fallidos,
    IReadOnlyCollection<MigracionArchivoResultDto> Detalle);

/// <summary>
/// Migración masiva de PDFs históricos (HU-04) — mismo extractor que la
/// carga individual (ver ADR-004), sin subirlos a MinIO (no se conserva el
/// archivo original migrado, solo los datos extraídos). Los Informes nacen
/// en Borrador con Origen=Migrado y sin CasoAnalisis de origen (el
/// histórico de Casos no se migra, ver docs/03-modelo-dominio.md). Solo
/// Administrador — el Gherkin de esta HU es explícito ("Como
/// Administrador"), a diferencia de HU-01/02/03 que también permiten
/// Analista/Supervisor.
/// </summary>
[Autorizar(Roles.Admin)]
public sealed record MigrarInformesCommand(Guid DependenciaDestinoId, IReadOnlyCollection<PdfMigrarDto> Pdfs)
    : IRequest<MigracionLoteResultDto>;
