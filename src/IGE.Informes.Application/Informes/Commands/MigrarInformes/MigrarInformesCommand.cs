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

/// <summary>
/// <paramref name="DestinoExtraido"/> es el texto crudo de "Destino" tal
/// como aparece escrito en el PDF (ej. "Comisaría 2da") — el parser lo
/// extrae pero el sistema no lo mapea a una Dependencia real del catálogo
/// (los nombres no siempre calzan exacto). Se muestra en el reporte para
/// que el Admin identifique de un vistazo a qué comisaría/dependencia
/// corresponde cada Informe migrado, sin tener que abrir el PDF, y así
/// corrija la Dependencia real desde la ficha del Informe después (HU-02).
/// Null si el PDF no pudo parsearse o no traía ese campo.
/// </summary>
public sealed record MigracionArchivoResultDto(
    string NombreArchivo, ResultadoMigracionArchivo Resultado, string? Motivo, string? DestinoExtraido = null);

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
/// histórico de Casos no se migra, ver docs/03-modelo-dominio.md).
/// Abierto a Analista/Supervisor/Admin (extensión 2026-08-12, pedido
/// explícito del usuario) — originalmente era exclusivo de Administrador.
/// </summary>
[Autorizar(Roles.Analista, Roles.Supervisor, Roles.Admin)]
public sealed record MigrarInformesCommand(Guid DependenciaDestinoId, IReadOnlyCollection<PdfMigrarDto> Pdfs)
    : IRequest<MigracionLoteResultDto>;
