using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IGE.Informes.UnitTests.TestDoubles;

/// <summary>
/// IAppDbContext de prueba respaldado por EF Core InMemory — permite
/// testear los Handlers de Application sin depender de Infrastructure ni
/// de una base de datos real (esa cobertura la dan los tests de
/// integración con Testcontainers).
/// </summary>
public sealed class TestAppDbContext : DbContext, IAppDbContext
{
    public TestAppDbContext()
        : base(new DbContextOptionsBuilder<TestAppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options)
    {
    }

    public DbSet<Dependencia> Dependencias => Set<Dependencia>();

    public DbSet<TipoIncidente> TiposIncidente => Set<TipoIncidente>();

    public DbSet<CasoAnalisis> CasosAnalisis => Set<CasoAnalisis>();

    public DbSet<Causa> Causas => Set<Causa>();

    public DbSet<Informe> Informes => Set<Informe>();

    public DbSet<CategoriaAlerta> CategoriasAlerta => Set<CategoriaAlerta>();

    public DbSet<Vehiculo> Vehiculos => Set<Vehiculo>();

    public DbSet<Persona> Personas => Set<Persona>();

    public DbSet<Camara> Camaras => Set<Camara>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<CasoAnalisis>(caso =>
        {
            caso.Metadata.FindNavigation(nameof(CasoAnalisis.Analistas))!
                .SetPropertyAccessMode(PropertyAccessMode.Field);

            caso.OwnsMany(c => c.Analistas, analistas =>
            {
                analistas.WithOwner().HasForeignKey(a => a.CasoId);
                analistas.HasKey(a => new { a.CasoId, a.UsuarioId });
            });
        });

        builder.Entity<Informe>(informe =>
        {
            informe.Metadata.FindNavigation(nameof(Informe.Analistas))!
                .SetPropertyAccessMode(PropertyAccessMode.Field);

            informe.OwnsMany(i => i.Analistas, analistas =>
            {
                analistas.WithOwner().HasForeignKey(a => a.InformeId);
                analistas.HasKey(a => new { a.InformeId, a.UsuarioId });
            });
        });

        builder.Entity<Vehiculo>(vehiculo =>
        {
            vehiculo.PrimitiveCollection(v => v.CategoriasAlertaIds)
                .Metadata.SetPropertyAccessMode(PropertyAccessMode.Field);
        });
    }
}
