using IGE.Informes.Application.Common.Behaviors;
using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Application.Common.Security;
using MediatR;

namespace IGE.Informes.UnitTests.Common.Behaviors;

public class AutorizacionBehaviorTests
{
    private sealed class FakeCurrentUserService(Guid? usuarioId, params string[] roles) : ICurrentUserService
    {
        public Guid? UsuarioId { get; } = usuarioId;
        public IReadOnlyCollection<string> Roles { get; } = roles;
    }

    [Autorizar(Application.Common.Security.Roles.Analista)]
    private sealed record RequestConAutorizacion : IRequest<string>;

    private sealed record RequestSinAutorizacion : IRequest<string>;

    private static Task<string> Ok(CancellationToken cancellationToken) => Task.FromResult("ok");

    [Fact]
    public async Task Rechaza_request_sin_atributo_Autorizar()
    {
        var behavior = new AutorizacionBehavior<RequestSinAutorizacion, string>(
            new FakeCurrentUserService(Guid.NewGuid(), Application.Common.Security.Roles.Admin));

        await Assert.ThrowsAsync<AutorizacionNoConfiguradaException>(() =>
            behavior.Handle(new RequestSinAutorizacion(), Ok, CancellationToken.None));
    }

    [Fact]
    public async Task Rechaza_usuario_no_autenticado()
    {
        var behavior = new AutorizacionBehavior<RequestConAutorizacion, string>(
            new FakeCurrentUserService(usuarioId: null));

        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            behavior.Handle(new RequestConAutorizacion(), Ok, CancellationToken.None));
    }

    [Fact]
    public async Task Rechaza_usuario_autenticado_sin_el_rol_requerido()
    {
        var behavior = new AutorizacionBehavior<RequestConAutorizacion, string>(
            new FakeCurrentUserService(Guid.NewGuid(), Application.Common.Security.Roles.Supervisor));

        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            behavior.Handle(new RequestConAutorizacion(), Ok, CancellationToken.None));
    }

    [Fact]
    public async Task Permite_usuario_autenticado_con_el_rol_requerido()
    {
        var behavior = new AutorizacionBehavior<RequestConAutorizacion, string>(
            new FakeCurrentUserService(Guid.NewGuid(), Application.Common.Security.Roles.Analista));

        var resultado = await behavior.Handle(new RequestConAutorizacion(), Ok, CancellationToken.None);

        Assert.Equal("ok", resultado);
    }
}
