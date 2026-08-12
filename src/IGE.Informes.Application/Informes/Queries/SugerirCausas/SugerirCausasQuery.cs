using IGE.Informes.Application.Common.Security;
using MediatR;

namespace IGE.Informes.Application.Informes.Queries.SugerirCausas;

public sealed record CausaSugeridaDto(
    Guid Id,
    string Caratula,
    string NroPiezaSumarial,
    string CircunscripcionJudicial);

/// <summary>
/// HU-02 · Editar / corregir metadatos de un informe (Épica 01), escenario
/// "Sugerir Causas existentes cuando la Pieza Sumarial no matchea ninguna".
/// Sugiere Causas ya existentes con carátula parecida a
/// <see cref="CaratulaAproximada"/> — se usa cuando el N° de Pieza Sumarial
/// ingresado en la edición de un Informe no coincide exacto con ninguna
/// Causa existente (ese caso se resuelve reusando la Causa directamente en
/// EditarInformeCommandHandler, sin pasar por esta Query). No decide
/// vinculación, solo devuelve candidatas para que el usuario elija.
/// </summary>
[Autorizar(Roles.Analista, Roles.Supervisor, Roles.Admin)]
public sealed record SugerirCausasQuery(string CaratulaAproximada) : IRequest<IReadOnlyCollection<CausaSugeridaDto>>;
