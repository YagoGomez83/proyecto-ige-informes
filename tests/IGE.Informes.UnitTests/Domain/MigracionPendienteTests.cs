using IGE.Informes.Domain.Entities;

namespace IGE.Informes.UnitTests.Domain;

/// <summary>
/// HU-04 · Migración histórica de informes desde Drive
/// (docs/epic-01-gestion-informes.md) — Característica "Migración masiva",
/// escenario "PDF con Fecha de Análisis no reconocida queda pendiente, no
/// se pierde". <see cref="MigracionPendiente"/> todavía no existe: estos
/// tests están escritos antes de la implementación (TDD), por lo que deben
/// fallar en rojo hasta que se cree
/// IGE.Informes.Domain.Entities.MigracionPendiente (ver
/// docs/03-modelo-dominio.md, sección "Decisiones ya resueltas").
/// </summary>
public class MigracionPendienteTests
{
    private static readonly Guid DependenciaDestinoId = Guid.NewGuid();
    private static readonly Guid UsuarioMigradorId = Guid.NewGuid();

    private static MigracionPendiente Crear(
        string idRegistro = "700/2022",
        string pdfPath = "migraciones-pendientes/700-2022.pdf",
        string? causaCaratula = null,
        string? piezaSumarial = null,
        string? relato = "Se procede a realizar el análisis histórico...") =>
        new(idRegistro, pdfPath, DependenciaDestinoId, UsuarioMigradorId, causaCaratula, piezaSumarial, relato);

    [Fact]
    public void Alta_guarda_el_IdRegistro_reconocido_y_el_PdfPath_en_MinIO()
    {
        var migracionPendiente = Crear(idRegistro: "700/2022", pdfPath: "migraciones-pendientes/700-2022.pdf");

        Assert.Equal("700/2022", migracionPendiente.IdRegistro);
        Assert.Equal("migraciones-pendientes/700-2022.pdf", migracionPendiente.PdfPath);
    }

    [Fact]
    public void Alta_guarda_la_DependenciaDestino_elegida_en_el_lote_y_el_usuario_que_ejecuto_la_migracion()
    {
        var migracionPendiente = Crear();

        Assert.Equal(DependenciaDestinoId, migracionPendiente.DependenciaDestinoId);
        Assert.Equal(UsuarioMigradorId, migracionPendiente.UsuarioMigradorId);
    }

    [Fact]
    public void Alta_acepta_Causa_y_Relato_opcionales_ya_extraidos_por_el_parser()
    {
        var migracionPendiente = Crear(causaCaratula: "AV. INFRACCION LEY 23.737", piezaSumarial: "7070029/26", relato: "Relato extraído");

        Assert.Equal("AV. INFRACCION LEY 23.737", migracionPendiente.CausaCaratula);
        Assert.Equal("7070029/26", migracionPendiente.PiezaSumarial);
        Assert.Equal("Relato extraído", migracionPendiente.Relato);
    }

    [Fact]
    public void Alta_sin_Causa_se_acepta_dado_que_el_parser_no_siempre_la_reconoce()
    {
        var migracionPendiente = Crear(causaCaratula: null, piezaSumarial: null);

        Assert.Null(migracionPendiente.CausaCaratula);
        Assert.Null(migracionPendiente.PiezaSumarial);
    }

    [Fact]
    public void Alta_rechaza_IdRegistro_vacio()
    {
        Assert.Throws<ArgumentException>(() => new MigracionPendiente(
            "", "migraciones-pendientes/x.pdf", DependenciaDestinoId, UsuarioMigradorId));
    }

    [Fact]
    public void Alta_rechaza_PdfPath_vacio()
    {
        Assert.Throws<ArgumentException>(() => new MigracionPendiente(
            "700/2022", "", DependenciaDestinoId, UsuarioMigradorId));
    }

    [Fact]
    public void Alta_rechaza_DependenciaDestino_vacia()
    {
        Assert.Throws<ArgumentException>(() => new MigracionPendiente(
            "700/2022", "migraciones-pendientes/x.pdf", Guid.Empty, UsuarioMigradorId));
    }

    [Fact]
    public void Alta_rechaza_UsuarioMigrador_vacio()
    {
        Assert.Throws<ArgumentException>(() => new MigracionPendiente(
            "700/2022", "migraciones-pendientes/x.pdf", DependenciaDestinoId, Guid.Empty));
    }

    [Fact]
    public void CompletarFechaAnalisis_permite_crear_el_Informe_real_con_los_mismos_datos_mas_la_fecha_ingresada()
    {
        var migracionPendiente = Crear(idRegistro: "700/2022", causaCaratula: "AV. INFRACCION LEY 23.737", piezaSumarial: "7070029/26");
        var fechaIngresadaPorElAdministrador = new DateOnly(2022, 6, 15);
        Guid? causaId = Guid.NewGuid();

        var informe = migracionPendiente.CrearInformeMigrado(fechaIngresadaPorElAdministrador, causaId);

        Assert.Equal("700/2022", informe.IdRegistro);
        Assert.Equal(fechaIngresadaPorElAdministrador, informe.FechaAnalisis);
        Assert.Equal(DependenciaDestinoId, informe.DependenciaDestinoId);
        Assert.Equal(OrigenInforme.Migrado, informe.Origen);
        Assert.Null(informe.CasoAnalisisId);
        Assert.Equal(EstadoInforme.Borrador, informe.Estado);
    }
}
