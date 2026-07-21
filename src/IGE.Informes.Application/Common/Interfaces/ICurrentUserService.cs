namespace IGE.Informes.Application.Common.Interfaces;

public interface ICurrentUserService
{
    Guid? UsuarioId { get; }

    IReadOnlyCollection<string> Roles { get; }
}
