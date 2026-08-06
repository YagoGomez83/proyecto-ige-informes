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

    DbSet<Barrio> Barrios { get; }

    DbSet<Localidad> Localidades { get; }

    DbSet<CentroControlCamaras> CentrosControlCamaras { get; }

    DbSet<TipoIncidente> TiposIncidente { get; }

    DbSet<CasoAnalisis> CasosAnalisis { get; }

    DbSet<Causa> Causas { get; }

    DbSet<Informe> Informes { get; }

    DbSet<MigracionPendiente> MigracionesPendientes { get; }

    DbSet<CategoriaAlerta> CategoriasAlerta { get; }

    DbSet<Vehiculo> Vehiculos { get; }

    DbSet<Persona> Personas { get; }

    DbSet<Camara> Camaras { get; }

    DbSet<Evidencia> Evidencias { get; }

    DbSet<VehiculoImagen> VehiculoImagenes { get; }

    DbSet<PersonaImagen> PersonaImagenes { get; }

    DbSet<PersonaVehiculo> PersonasVehiculo { get; }

    DbSet<Alerta> Alertas { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Fuerza el estado "Agregado" para un InformeAnalista recién añadido a
    /// Informe.Analistas (owned collection) de un Informe ya persistido.
    /// Necesario por un bug conocido de EF Core (10.x) con OwnsMany: al
    /// agregar un elemento nuevo a una owned collection de una entidad ya
    /// trackeada/persistida, el change tracker a veces lo clasifica como
    /// "Modified" en vez de "Added", generando un UPDATE sobre una fila
    /// inexistente (DbUpdateConcurrencyException). Acotado a InformeAnalista
    /// a propósito — no es un bypass genérico del ChangeTracker para
    /// cualquier entidad, solo el workaround puntual de este bug.
    /// </summary>
    void MarcarInformeAnalistaComoAgregado(InformeAnalista informeAnalista);
}
