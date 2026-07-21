using IGE.Informes.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IGE.Informes.Application.Common.Interfaces;

/// <summary>
/// Puerto hacia la persistencia: Application depende solo de esta interfaz,
/// nunca de EF Core/Infrastructure directamente (Clean Architecture).
/// </summary>
public interface IAppDbContext
{
    DbSet<Dependencia> Dependencias { get; }

    DbSet<TipoIncidente> TiposIncidente { get; }

    DbSet<CasoAnalisis> CasosAnalisis { get; }

    DbSet<Causa> Causas { get; }

    DbSet<Informe> Informes { get; }

    DbSet<CategoriaAlerta> CategoriasAlerta { get; }

    DbSet<Vehiculo> Vehiculos { get; }

    DbSet<Persona> Personas { get; }

    DbSet<Camara> Camaras { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
