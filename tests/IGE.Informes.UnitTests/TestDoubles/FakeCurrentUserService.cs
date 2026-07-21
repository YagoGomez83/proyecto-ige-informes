using IGE.Informes.Application.Common.Interfaces;

namespace IGE.Informes.UnitTests.TestDoubles;

public sealed class FakeCurrentUserService(Guid? usuarioId, params string[] roles) : ICurrentUserService
{
    public Guid? UsuarioId { get; } = usuarioId;
    public IReadOnlyCollection<string> Roles { get; } = roles;
}
