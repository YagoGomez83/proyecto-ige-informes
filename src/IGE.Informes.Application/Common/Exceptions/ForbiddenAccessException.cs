namespace IGE.Informes.Application.Common.Exceptions;

/// <summary>
/// El usuario actual no tiene ninguno de los roles requeridos para ejecutar
/// el Command/Query, o no está autenticado.
/// </summary>
public sealed class ForbiddenAccessException(string message) : Exception(message);
