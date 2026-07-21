namespace IGE.Informes.Application.Common.Exceptions;

public sealed class EntidadNoEncontradaException(string entidad, Guid id)
    : Exception($"No se encontró '{entidad}' con id '{id}'.");
