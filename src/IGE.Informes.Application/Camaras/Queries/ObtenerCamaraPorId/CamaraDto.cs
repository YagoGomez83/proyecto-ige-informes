using IGE.Informes.Domain.Entities;

namespace IGE.Informes.Application.Camaras.Queries.ObtenerCamaraPorId;

public sealed record CamaraDto(Guid Id, string Codigo, TipoCamara Tipo, string? Ubicacion);
