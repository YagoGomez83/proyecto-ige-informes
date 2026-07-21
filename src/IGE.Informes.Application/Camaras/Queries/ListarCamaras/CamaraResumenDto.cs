using IGE.Informes.Domain.Entities;

namespace IGE.Informes.Application.Camaras.Queries.ListarCamaras;

public sealed record CamaraResumenDto(Guid Id, string Codigo, TipoCamara Tipo, bool PendienteDeUbicacion);
