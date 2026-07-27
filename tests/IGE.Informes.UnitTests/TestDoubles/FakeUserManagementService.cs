using IGE.Informes.Application.Common.Interfaces;

namespace IGE.Informes.UnitTests.TestDoubles;

/// <summary>
/// Fake manual de <see cref="IUserManagementService"/> con estado en memoria,
/// suficiente para que los Handlers de la HU-17 (Gestión de Usuarios) se
/// prueben de punta a punta sin depender de ASP.NET Core Identity real.
/// El proyecto no usa librerías de mocking (ver CLAUDE.md) — todo es fakes
/// manuales, en el mismo estilo que <see cref="FakeCurrentUserService"/>.
/// </summary>
public sealed class FakeUserManagementService : IUserManagementService
{
    private sealed class UsuarioInterno
    {
        public required Guid Id { get; init; }
        public required string NombreCompleto { get; set; }
        public required string Email { get; set; }
        public required string Rol { get; set; }
        public bool Bloqueado { get; set; }
    }

    private readonly Dictionary<Guid, UsuarioInterno> _usuarios = [];

    /// <summary>
    /// Longitud mínima de contraseña que el fake considera válida, simulando
    /// la política mínima de Identity (ver HU-17: 12 caracteres o más).
    /// </summary>
    public int LongitudMinimaPassword { get; set; } = 12;

    public IReadOnlyCollection<Guid> BloquearAsyncLlamadoCon => _bloquearAsyncLlamadoCon;
    private readonly List<Guid> _bloquearAsyncLlamadoCon = [];

    public IReadOnlyCollection<Guid> DesbloquearAsyncLlamadoCon => _desbloquearAsyncLlamadoCon;
    private readonly List<Guid> _desbloquearAsyncLlamadoCon = [];

    public IReadOnlyCollection<(Guid UsuarioId, string NuevoRol)> CambiarRolAsyncLlamadoCon => _cambiarRolAsyncLlamadoCon;
    private readonly List<(Guid UsuarioId, string NuevoRol)> _cambiarRolAsyncLlamadoCon = [];

    /// <summary>
    /// Helper de setup para pruebas: agrega un usuario preexistente sin pasar
    /// por <see cref="CrearUsuarioAsync"/>.
    /// </summary>
    public Guid AgregarUsuarioExistente(string nombreCompleto, string email, string rol, bool bloqueado = false)
    {
        var id = Guid.NewGuid();
        _usuarios[id] = new UsuarioInterno
        {
            Id = id,
            NombreCompleto = nombreCompleto,
            Email = email,
            Rol = rol,
            Bloqueado = bloqueado,
        };
        return id;
    }

    public Task<IReadOnlyCollection<UsuarioDto>> ListarUsuariosAsync(CancellationToken cancellationToken)
    {
        IReadOnlyCollection<UsuarioDto> resultado = _usuarios.Values
            .Select(u => new UsuarioDto(u.Id, u.NombreCompleto, u.Email, u.Rol, u.Bloqueado))
            .ToList();

        return Task.FromResult(resultado);
    }

    public Task<bool> ExisteEmailAsync(string email, CancellationToken cancellationToken)
    {
        var existe = _usuarios.Values.Any(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(existe);
    }

    public Task<Guid?> CrearUsuarioAsync(string nombreCompleto, string email, string password, string rol, CancellationToken cancellationToken)
    {
        if (password.Length < LongitudMinimaPassword)
        {
            return Task.FromResult<Guid?>(null);
        }

        var id = Guid.NewGuid();
        _usuarios[id] = new UsuarioInterno
        {
            Id = id,
            NombreCompleto = nombreCompleto,
            Email = email,
            Rol = rol,
            Bloqueado = false,
        };

        return Task.FromResult<Guid?>(id);
    }

    public Task<bool> ExisteUsuarioAsync(Guid usuarioId, CancellationToken cancellationToken)
    {
        return Task.FromResult(_usuarios.ContainsKey(usuarioId));
    }

    public Task CambiarRolAsync(Guid usuarioId, string nuevoRol, CancellationToken cancellationToken)
    {
        _cambiarRolAsyncLlamadoCon.Add((usuarioId, nuevoRol));

        if (_usuarios.TryGetValue(usuarioId, out var usuario))
        {
            usuario.Rol = nuevoRol;
        }

        return Task.CompletedTask;
    }

    public Task BloquearAsync(Guid usuarioId, CancellationToken cancellationToken)
    {
        _bloquearAsyncLlamadoCon.Add(usuarioId);

        if (_usuarios.TryGetValue(usuarioId, out var usuario))
        {
            usuario.Bloqueado = true;
        }

        return Task.CompletedTask;
    }

    public Task DesbloquearAsync(Guid usuarioId, CancellationToken cancellationToken)
    {
        _desbloquearAsyncLlamadoCon.Add(usuarioId);

        if (_usuarios.TryGetValue(usuarioId, out var usuario))
        {
            usuario.Bloqueado = false;
        }

        return Task.CompletedTask;
    }
}
