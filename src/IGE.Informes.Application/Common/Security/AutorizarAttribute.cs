namespace IGE.Informes.Application.Common.Security;

/// <summary>
/// Declara qué roles pueden ejecutar un Command/Query. Obligatorio en todo
/// Command/Query: <see cref="Behaviors.AutorizacionBehavior{TRequest,TResponse}"/>
/// rechaza cualquier request que no lo tenga (fail-closed).
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class AutorizarAttribute(params string[] roles) : Attribute
{
    public IReadOnlyCollection<string> Roles { get; } = roles;
}
