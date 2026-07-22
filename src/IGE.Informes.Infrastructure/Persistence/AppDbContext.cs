using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Domain.Entities;
using IGE.Informes.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace IGE.Informes.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(options), IAppDbContext
{
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<Dependencia> Dependencias => Set<Dependencia>();

    public DbSet<TipoIncidente> TiposIncidente => Set<TipoIncidente>();

    public DbSet<CasoAnalisis> CasosAnalisis => Set<CasoAnalisis>();

    public DbSet<Causa> Causas => Set<Causa>();

    public DbSet<Informe> Informes => Set<Informe>();

    public DbSet<CategoriaAlerta> CategoriasAlerta => Set<CategoriaAlerta>();

    public DbSet<Vehiculo> Vehiculos => Set<Vehiculo>();

    public DbSet<Persona> Personas => Set<Persona>();

    public DbSet<Camara> Camaras => Set<Camara>();

    public DbSet<Evidencia> Evidencias => Set<Evidencia>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    public void MarcarInformeAnalistaComoAgregado(InformeAnalista informeAnalista)
    {
        Entry(informeAnalista).State = EntityState.Added;
    }
}
