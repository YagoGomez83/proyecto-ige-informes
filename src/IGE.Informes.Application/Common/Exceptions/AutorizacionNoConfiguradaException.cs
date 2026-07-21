namespace IGE.Informes.Application.Common.Exceptions;

/// <summary>
/// Error de desarrollo, no de usuario: un Command/Query no declaró
/// <see cref="Security.AutorizarAttribute"/>. Fail-closed intencional — ver
/// ADR de autorización server-side (docs/06-seguridad-amenazas.md, A01).
/// </summary>
public sealed class AutorizacionNoConfiguradaException(Type requestType)
    : Exception($"'{requestType.Name}' no declara [Autorizar(...)]. Todo Command/Query debe declarar explícitamente los roles permitidos.");
