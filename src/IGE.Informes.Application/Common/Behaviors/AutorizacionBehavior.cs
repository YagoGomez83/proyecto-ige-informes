using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Application.Common.Security;
using MediatR;

namespace IGE.Informes.Application.Common.Behaviors;

/// <summary>
/// RBAC server-side obligatorio para todo Command/Query (docs/06-seguridad-amenazas.md,
/// A01 Broken Access Control). Fail-closed: un request sin [Autorizar(...)]
/// se rechaza en vez de dejarse pasar.
/// </summary>
public sealed class AutorizacionBehavior<TRequest, TResponse>(ICurrentUserService currentUserService)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var atributo = typeof(TRequest).GetCustomAttributes(typeof(AutorizarAttribute), inherit: true)
            .Cast<AutorizarAttribute>()
            .SingleOrDefault()
            ?? throw new AutorizacionNoConfiguradaException(typeof(TRequest));

        if (currentUserService.UsuarioId is null)
        {
            throw new ForbiddenAccessException("No hay un usuario autenticado.");
        }

        var tieneRolPermitido = currentUserService.Roles.Any(atributo.Roles.Contains);
        if (!tieneRolPermitido)
        {
            throw new ForbiddenAccessException(
                $"El usuario no tiene ninguno de los roles requeridos ({string.Join(", ", atributo.Roles)}) para ejecutar '{typeof(TRequest).Name}'.");
        }

        return next();
    }
}
