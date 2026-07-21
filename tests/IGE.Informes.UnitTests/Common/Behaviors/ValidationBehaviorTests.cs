using FluentValidation;
using IGE.Informes.Application.Common.Behaviors;
using MediatR;

namespace IGE.Informes.UnitTests.Common.Behaviors;

public class ValidationBehaviorTests
{
    private sealed record UnRequest(string Nombre) : IRequest<string>;

    private sealed class RequestValidator : AbstractValidator<UnRequest>
    {
        public RequestValidator()
        {
            RuleFor(x => x.Nombre).NotEmpty();
        }
    }

    private static Task<string> Ok(CancellationToken cancellationToken) => Task.FromResult("ok");

    [Fact]
    public async Task Deja_pasar_un_request_valido()
    {
        var behavior = new ValidationBehavior<UnRequest, string>([new RequestValidator()]);

        var resultado = await behavior.Handle(new UnRequest("algo"), Ok, CancellationToken.None);

        Assert.Equal("ok", resultado);
    }

    [Fact]
    public async Task Rechaza_un_request_invalido_con_ValidationException()
    {
        var behavior = new ValidationBehavior<UnRequest, string>([new RequestValidator()]);

        await Assert.ThrowsAsync<ValidationException>(() =>
            behavior.Handle(new UnRequest(""), Ok, CancellationToken.None));
    }

    [Fact]
    public async Task Sin_validators_registrados_deja_pasar_directo()
    {
        var behavior = new ValidationBehavior<UnRequest, string>([]);

        var resultado = await behavior.Handle(new UnRequest(""), Ok, CancellationToken.None);

        Assert.Equal("ok", resultado);
    }

    [Fact]
    public async Task Acumula_errores_de_multiples_validators()
    {
        var otroValidator = new InlineValidator<UnRequest>();
        otroValidator.RuleFor(x => x.Nombre).MinimumLength(10);

        var behavior = new ValidationBehavior<UnRequest, string>([new RequestValidator(), otroValidator]);

        var excepcion = await Assert.ThrowsAsync<ValidationException>(() =>
            behavior.Handle(new UnRequest(""), Ok, CancellationToken.None));

        Assert.True(excepcion.Errors.Count() >= 1);
    }
}
