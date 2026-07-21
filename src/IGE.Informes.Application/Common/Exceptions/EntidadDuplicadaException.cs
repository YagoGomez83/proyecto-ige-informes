namespace IGE.Informes.Application.Common.Exceptions;

public sealed class EntidadDuplicadaException(string entidad, string campo, string valor)
    : Exception($"Ya existe '{entidad}' con {campo} = '{valor}'.");
