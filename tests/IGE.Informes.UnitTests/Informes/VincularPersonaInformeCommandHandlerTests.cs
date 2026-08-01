using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.Informes.Commands.VincularPersonaInforme;
using IGE.Informes.Domain.Entities;
using IGE.Informes.UnitTests.TestDoubles;

namespace IGE.Informes.UnitTests.Informes;

public class VincularPersonaInformeCommandHandlerTests
{
    private static Informe CrearInforme() =>
        Informe.CrearMigrado("290/2026", new DateOnly(2026, 7, 21), Guid.NewGuid(), Guid.NewGuid());

    private static Persona CrearPersona() => new(RolPersona.Testigo, nombre: "Juan Pérez");

    [Fact]
    public async Task Vincula_la_persona_al_informe_arrancando_el_numero_de_imagen_en_1()
    {
        var dbContext = new TestAppDbContext();
        var informe = CrearInforme();
        var persona = CrearPersona();
        dbContext.Informes.Add(informe);
        dbContext.Personas.Add(persona);
        await dbContext.SaveChangesAsync();

        var handler = new VincularPersonaInformeCommandHandler(dbContext);
        await handler.Handle(new VincularPersonaInformeCommand(informe.Id, persona.Id), CancellationToken.None);

        var evidencia = Assert.Single(dbContext.Evidencias);
        Assert.Equal(1, evidencia.NumeroImagen);
        Assert.Contains(persona.Id, evidencia.PersonaIds);
    }

    [Fact]
    public async Task Autoasigna_el_siguiente_numero_de_imagen_si_ya_hay_evidencias_del_pdf()
    {
        var dbContext = new TestAppDbContext();
        var informe = CrearInforme();
        var persona = CrearPersona();
        dbContext.Informes.Add(informe);
        dbContext.Personas.Add(persona);
        dbContext.Evidencias.Add(new Evidencia(1, informe.Id));
        dbContext.Evidencias.Add(new Evidencia(2, informe.Id));
        await dbContext.SaveChangesAsync();

        var handler = new VincularPersonaInformeCommandHandler(dbContext);
        await handler.Handle(new VincularPersonaInformeCommand(informe.Id, persona.Id), CancellationToken.None);

        var nueva = dbContext.Evidencias.Single(e => e.PersonaIds.Contains(persona.Id));
        Assert.Equal(3, nueva.NumeroImagen);
    }

    [Fact]
    public async Task Vincular_la_misma_persona_dos_veces_es_idempotente()
    {
        var dbContext = new TestAppDbContext();
        var informe = CrearInforme();
        var persona = CrearPersona();
        dbContext.Informes.Add(informe);
        dbContext.Personas.Add(persona);
        await dbContext.SaveChangesAsync();

        var handler = new VincularPersonaInformeCommandHandler(dbContext);
        await handler.Handle(new VincularPersonaInformeCommand(informe.Id, persona.Id), CancellationToken.None);
        await handler.Handle(new VincularPersonaInformeCommand(informe.Id, persona.Id), CancellationToken.None);

        Assert.Single(dbContext.Evidencias);
    }

    [Fact]
    public async Task Rechaza_un_informe_inexistente()
    {
        var dbContext = new TestAppDbContext();
        var persona = CrearPersona();
        dbContext.Personas.Add(persona);
        await dbContext.SaveChangesAsync();

        var handler = new VincularPersonaInformeCommandHandler(dbContext);

        await Assert.ThrowsAsync<EntidadNoEncontradaException>(() => handler.Handle(
            new VincularPersonaInformeCommand(Guid.NewGuid(), persona.Id), CancellationToken.None));
    }

    [Fact]
    public async Task Rechaza_una_persona_inexistente()
    {
        var dbContext = new TestAppDbContext();
        var informe = CrearInforme();
        dbContext.Informes.Add(informe);
        await dbContext.SaveChangesAsync();

        var handler = new VincularPersonaInformeCommandHandler(dbContext);

        await Assert.ThrowsAsync<EntidadNoEncontradaException>(() => handler.Handle(
            new VincularPersonaInformeCommand(informe.Id, Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Rechaza_vincular_a_un_informe_publicado()
    {
        var dbContext = new TestAppDbContext();
        var informe = new Informe("290/2026", new DateOnly(2026, 7, 21), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        informe.AgregarFirmante(Guid.NewGuid());
        informe.Publicar();
        var persona = CrearPersona();
        dbContext.Informes.Add(informe);
        dbContext.Personas.Add(persona);
        await dbContext.SaveChangesAsync();

        var handler = new VincularPersonaInformeCommandHandler(dbContext);

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(
            new VincularPersonaInformeCommand(informe.Id, persona.Id), CancellationToken.None));
    }

    [Fact]
    public async Task Vincular_una_persona_ya_vinculada_a_otro_informe_genera_Alerta_de_reincidencia()
    {
        var dbContext = new TestAppDbContext();
        var informeAnterior = CrearInforme();
        var informeNuevo = CrearInforme();
        var persona = CrearPersona();
        dbContext.Informes.Add(informeAnterior);
        dbContext.Informes.Add(informeNuevo);
        dbContext.Personas.Add(persona);

        var evidenciaAnterior = new Evidencia(1, informeAnterior.Id);
        evidenciaAnterior.VincularPersona(persona.Id);
        dbContext.Evidencias.Add(evidenciaAnterior);
        await dbContext.SaveChangesAsync();

        var handler = new VincularPersonaInformeCommandHandler(dbContext);
        await handler.Handle(new VincularPersonaInformeCommand(informeNuevo.Id, persona.Id), CancellationToken.None);

        var alerta = Assert.Single(dbContext.Alertas);
        Assert.Equal(TipoAlerta.ReincidenciaOtroInforme, alerta.Tipo);
        Assert.Equal(persona.Id, alerta.PersonaId);
        Assert.Equal(informeNuevo.Id, alerta.InformeId);
        Assert.Equal(informeAnterior.Id, alerta.InformePrevioId);
    }

    [Fact]
    public async Task Vincular_una_persona_sin_vinculo_previo_genera_Alerta_de_carga_huerfana()
    {
        var dbContext = new TestAppDbContext();
        var informe = CrearInforme();
        var persona = CrearPersona();
        dbContext.Informes.Add(informe);
        dbContext.Personas.Add(persona);
        await dbContext.SaveChangesAsync();

        var handler = new VincularPersonaInformeCommandHandler(dbContext);
        await handler.Handle(new VincularPersonaInformeCommand(informe.Id, persona.Id), CancellationToken.None);

        var alerta = Assert.Single(dbContext.Alertas);
        Assert.Equal(TipoAlerta.CargaHuerfana, alerta.Tipo);
        Assert.Equal(persona.Id, alerta.PersonaId);
        Assert.Equal(informe.Id, alerta.InformeId);
        Assert.Null(alerta.InformePrevioId);
    }

    [Fact]
    public async Task Vincular_la_misma_persona_dos_veces_al_mismo_informe_no_genera_Alerta_duplicada()
    {
        var dbContext = new TestAppDbContext();
        var informe = CrearInforme();
        var persona = CrearPersona();
        dbContext.Informes.Add(informe);
        dbContext.Personas.Add(persona);
        await dbContext.SaveChangesAsync();

        var handler = new VincularPersonaInformeCommandHandler(dbContext);
        await handler.Handle(new VincularPersonaInformeCommand(informe.Id, persona.Id), CancellationToken.None);
        await handler.Handle(new VincularPersonaInformeCommand(informe.Id, persona.Id), CancellationToken.None);

        Assert.Single(dbContext.Alertas);
    }
}
