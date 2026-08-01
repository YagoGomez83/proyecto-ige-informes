using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Domain.Entities;
using IGE.Informes.Infrastructure.Busqueda;
using IGE.Informes.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace IGE.Informes.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(options), IAppDbContext
{
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<Dependencia> Dependencias => Set<Dependencia>();

    public DbSet<Barrio> Barrios => Set<Barrio>();

    public DbSet<Localidad> Localidades => Set<Localidad>();

    public DbSet<CentroControlCamaras> CentrosControlCamaras => Set<CentroControlCamaras>();

    public DbSet<TipoIncidente> TiposIncidente => Set<TipoIncidente>();

    public DbSet<CasoAnalisis> CasosAnalisis => Set<CasoAnalisis>();

    public DbSet<Causa> Causas => Set<Causa>();

    public DbSet<Informe> Informes => Set<Informe>();

    public DbSet<CategoriaAlerta> CategoriasAlerta => Set<CategoriaAlerta>();

    public DbSet<Vehiculo> Vehiculos => Set<Vehiculo>();

    public DbSet<Persona> Personas => Set<Persona>();

    public DbSet<Camara> Camaras => Set<Camara>();

    public DbSet<Evidencia> Evidencias => Set<Evidencia>();

    public DbSet<VehiculoImagen> VehiculoImagenes => Set<VehiculoImagen>();

    public DbSet<PersonaImagen> PersonaImagenes => Set<PersonaImagen>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Extensión unaccent: la búsqueda combinada (extensión de HU-05 sobre
        // Vehiculo/Persona/CasoAnalisis) usa EF.Functions.ILike + unaccent()
        // para que buscar "perez" encuentre "Pérez" — sin esto, ILike es
        // case-insensitive pero sensible a acentos.
        builder.HasPostgresExtension("unaccent");

        // El [DbFunction] de FuncionesPostgres.Unaccent no se descubre por
        // convención porque no está referenciado desde ningún mapeo de
        // entidad — hay que registrarlo explícitamente acá.
        builder.HasDbFunction(typeof(FuncionesPostgres).GetMethod(nameof(FuncionesPostgres.Unaccent), [typeof(string)])!)
            .HasName("unaccent");

        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    public void MarcarInformeAnalistaComoAgregado(InformeAnalista informeAnalista)
    {
        Entry(informeAnalista).State = EntityState.Added;
    }
}
