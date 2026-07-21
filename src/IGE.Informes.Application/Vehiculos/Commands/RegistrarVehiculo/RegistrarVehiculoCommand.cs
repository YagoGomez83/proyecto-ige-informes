using IGE.Informes.Application.Common.Security;
using IGE.Informes.Domain.Entities;
using MediatR;

namespace IGE.Informes.Application.Vehiculos.Commands.RegistrarVehiculo;

[Autorizar(Roles.Analista, Roles.Supervisor, Roles.Admin)]
public sealed record RegistrarVehiculoCommand(
    string Marca,
    string Modelo,
    string Color,
    CertezaDominio DominioCerteza,
    AccionARealizar AccionARealizar,
    string AvisarA,
    string? Dominio,
    string? Caracteristicas,
    IReadOnlyCollection<Guid> CategoriasAlertaIds) : IRequest<Guid>;
