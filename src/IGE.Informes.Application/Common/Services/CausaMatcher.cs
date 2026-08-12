using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IGE.Informes.Application.Common.Services;

/// <summary>
/// Matching de Causa por N° de Pieza Sumarial exacto (no la carátula, que
/// es texto libre con variaciones de transcripción entre PDFs del mismo
/// expediente) — evita crear una Causa duplicada cuando dos Informes
/// distintos citan el mismo expediente judicial real (ver
/// docs/03-modelo-dominio.md, "Decisiones ya resueltas"). Compartido entre
/// EditarInformeCommandHandler (edición manual), MigrarInformesCommandHandler
/// y CrearInformeDesdeMigracionPendienteCommandHandler (migración masiva) —
/// mismo criterio en los tres puntos donde se persiste una Causa nueva.
/// </summary>
public static class CausaMatcher
{
    /// <summary>
    /// Busca una Causa existente con el mismo N° de Pieza Sumarial exacto.
    /// No crea nada — para usar donde no se dispone de los 3 campos
    /// completos de Causa (ej. la migración masiva: el parser de PDF nunca
    /// extrae Circunscripción judicial, ver skill pdf-informe-parser) y
    /// solo tiene sentido vincular si ya existe, nunca crear una Causa
    /// nueva incompleta.
    /// </summary>
    public static Task<Causa?> BuscarPorPiezaSumarialAsync(
        IAppDbContext dbContext, string nroPiezaSumarial, CancellationToken cancellationToken) =>
        dbContext.Causas.FirstOrDefaultAsync(c => c.NroPiezaSumarial == nroPiezaSumarial, cancellationToken);

    /// <summary>
    /// Busca una Causa existente con el mismo N° de Pieza Sumarial exacto;
    /// si no hay match, crea una nueva y la agrega al DbContext. En ambos
    /// casos devuelve el Id a usar para vincular el Informe. Solo se llama
    /// cuando los 3 campos están completos — Causa exige carátula, pieza
    /// sumarial y circunscripción judicial no vacíos (constructor de
    /// Domain).
    /// </summary>
    public static async Task<Guid> ObtenerOCrearIdAsync(
        IAppDbContext dbContext,
        string caratula,
        string nroPiezaSumarial,
        string circunscripcionJudicial,
        CancellationToken cancellationToken)
    {
        var causaExistente = await BuscarPorPiezaSumarialAsync(dbContext, nroPiezaSumarial, cancellationToken);

        if (causaExistente is not null)
        {
            return causaExistente.Id;
        }

        var causa = new Causa(caratula, nroPiezaSumarial, circunscripcionJudicial);
        dbContext.Causas.Add(causa);
        return causa.Id;
    }
}
