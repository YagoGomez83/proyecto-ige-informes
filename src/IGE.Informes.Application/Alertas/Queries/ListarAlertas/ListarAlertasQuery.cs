using IGE.Informes.Application.Common.Dtos;
using IGE.Informes.Application.Common.Security;
using IGE.Informes.Domain.Entities;
using MediatR;

namespace IGE.Informes.Application.Alertas.Queries.ListarAlertas;

[Autorizar(Roles.Analista, Roles.Supervisor, Roles.Admin)]
public sealed record ListarAlertasQuery(int Pagina = 1, int TamanioPagina = 20, bool? SoloNoAtendidas = null)
    : IRequest<PagedResult<AlertaDto>>;

public sealed record AlertaDto(
    Guid Id,
    TipoAlerta Tipo,
    Guid? VehiculoId,
    string? VehiculoResumen,
    Guid? PersonaId,
    string? PersonaResumen,
    Guid InformeId,
    string InformeIdRegistro,
    Guid? InformePrevioId,
    string? InformePrevioIdRegistro,
    DateTime FechaGeneracion,
    bool Atendida);
