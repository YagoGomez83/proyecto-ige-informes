using IGE.Informes.Application.Common.Security;
using MediatR;

namespace IGE.Informes.Application.Vehiculos.Commands.AsignarCategoriaAlerta;

[Autorizar(Roles.Analista, Roles.Supervisor, Roles.Admin)]
public sealed record AsignarCategoriaAlertaCommand(Guid VehiculoId, Guid CategoriaAlertaId) : IRequest;
