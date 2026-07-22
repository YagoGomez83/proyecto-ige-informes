using IGE.Informes.Application.Common.Security;
using MediatR;

namespace IGE.Informes.Application.Informes.Queries.ParsearPdfInforme;

/// <summary>
/// Solo parsea el PDF y consulta duplicados — no persiste nada. El
/// resultado alimenta la pantalla de confirmación de HU-01; el guardado
/// real ocurre en ConfirmarCargaInformeCommand.
/// </summary>
[Autorizar(Roles.Analista, Roles.Supervisor, Roles.Admin)]
public sealed record ParsearPdfInformeQuery(byte[] ContenidoPdf) : IRequest<ParsearPdfInformeResultDto>;
