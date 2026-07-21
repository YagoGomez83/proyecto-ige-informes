using FluentValidation;
using MediatR;

namespace IGE.Informes.Application.Common.Behaviors;

/// <summary>
/// Ejecuta todos los FluentValidation.Validator registrados para el
/// request antes del Handler (A03 Injection / validación de entrada,
/// docs/06-seguridad-amenazas.md). Sin este behavior, un Validator definido
/// junto al Command nunca se invocaba.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);

        var errores = (await Task.WhenAll(validators.Select(v => v.ValidateAsync(context, cancellationToken))))
            .SelectMany(resultado => resultado.Errors)
            .Where(error => error is not null)
            .ToList();

        if (errores.Count > 0)
        {
            throw new ValidationException(errores);
        }

        return await next();
    }
}
