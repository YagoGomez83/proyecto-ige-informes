using IGE.Informes.Application.Common.Security;
using MediatR;

namespace IGE.Informes.Application.Informes.Commands.PublicarInforme;

/// <summary>
/// Publica un Informe en Borrador (HU-03). Si el usuario actual todavía no
/// figura como Analista Firmante de ese Informe, se lo agrega
/// automáticamente antes de publicar — un solo click publica y firma a la
/// vez. Un Informe ya Publicado no admite volver a firmarlo ni republicarse
/// (es inmutable, ver Informe.Publicar()).
/// </summary>
[Autorizar(Roles.Analista, Roles.Supervisor, Roles.Admin)]
public sealed record PublicarInformeCommand(Guid InformeId) : IRequest;
