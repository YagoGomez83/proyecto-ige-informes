using IGE.Informes.Domain.Entities;

namespace IGE.Informes.UnitTests.Domain;

/// <summary>
/// HU-04 · Migración histórica de informes desde Drive
/// (docs/epic-01-gestion-informes.md) — Característica "Migración masiva",
/// escenarios "PDF con Fecha de Análisis no reconocida queda pendiente, no
/// se pierde" y "PDF con ID Registro no reconocido también queda
/// pendiente, no se pierde". A partir de este segundo escenario,
/// <see cref="MigracionPendiente.IdRegistro"/> pasa a ser <c>string?</c>
/// (nullable) — ver docs/03-modelo-dominio.md, "Decisiones ya resueltas",
/// párrafo "MigracionPendiente.IdRegistro es nullable". Estos tests están
/// escritos antes de la implementación (TDD): los que dan de alta sin
/// IdRegistro deben fallar en rojo hasta que el constructor deje de exigir
/// idRegistro obligatorio.
/// </summary>
public class MigracionPendienteTests
{
    private static readonly Guid DependenciaDestinoId = Guid.NewGuid();
    private static readonly Guid UsuarioMigradorId = Guid.NewGuid();

    private static MigracionPendiente Crear(
        string? idRegistro = "700/2022",
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
    public void Alta_rechaza_IdRegistro_vacio_o_solo_espacios_pero_no_null()
    {
        // "" o "   " siguen siendo un dato inválido (el parser nunca
        // extraería eso) — se distingue de null, que ahora es un valor
        // válido y explícito ("no se reconoció ID Registro").
        Assert.Throws<ArgumentException>(() => new MigracionPendiente(
            "", "migraciones-pendientes/x.pdf", DependenciaDestinoId, UsuarioMigradorId));
        Assert.Throws<ArgumentException>(() => new MigracionPendiente(
            "   ", "migraciones-pendientes/x.pdf", DependenciaDestinoId, UsuarioMigradorId));
    }

    [Fact]
    public void Alta_sin_IdRegistro_se_acepta_para_el_escenario_de_ID_Registro_no_reconocido()
    {
        var migracionPendiente = Crear(idRegistro: null);

        Assert.Null(migracionPendiente.IdRegistro);
    }

    [Fact]
    public void Alta_sin_IdRegistro_sigue_guardando_el_PdfPath_y_los_demas_datos_ya_extraidos()
    {
        var migracionPendiente = Crear(
            idRegistro: null,
            pdfPath: "migraciones-pendientes/sin-id.pdf",
            causaCaratula: "AV. INFRACCION LEY 23.737",
            piezaSumarial: "7070029/26",
            relato: "Relato sin ID Registro reconocido");

        Assert.Null(migracionPendiente.IdRegistro);
        Assert.Equal("migraciones-pendientes/sin-id.pdf", migracionPendiente.PdfPath);
        Assert.Equal("AV. INFRACCION LEY 23.737", migracionPendiente.CausaCaratula);
        Assert.Equal("7070029/26", migracionPendiente.PiezaSumarial);
        Assert.Equal("Relato sin ID Registro reconocido", migracionPendiente.Relato);
        Assert.Equal(DependenciaDestinoId, migracionPendiente.DependenciaDestinoId);
        Assert.Equal(UsuarioMigradorId, migracionPendiente.UsuarioMigradorId);
    }

    [Fact]
    public void Alta_con_IdRegistro_presente_sigue_funcionando_igual_que_antes()
    {
        var migracionPendiente = Crear(idRegistro: "700/2022");

        Assert.Equal("700/2022", migracionPendiente.IdRegistro);
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

        var informe = migracionPendiente.CrearInformeMigrado(fechaIngresadaPorElAdministrador, causaId: causaId);

        Assert.Equal("700/2022", informe.IdRegistro);
        Assert.Equal(fechaIngresadaPorElAdministrador, informe.FechaAnalisis);
        Assert.Equal(DependenciaDestinoId, informe.DependenciaDestinoId);
        Assert.Equal(OrigenInforme.Migrado, informe.Origen);
        Assert.Null(informe.CasoAnalisisId);
        Assert.Equal(EstadoInforme.Borrador, informe.Estado);
    }

    [Fact]
    public void CompletarIdRegistro_ConMigracionPendienteSinIdRegistro_CreaElInformeConElIdRegistroIngresado()
    {
        // Escenario Gherkin "Completar el ID Registro de una Migración
        // Pendiente": si la MigracionPendiente no tenía IdRegistro
        // reconocido, el que ingresa el Administrador se usa para crear el
        // Informe real.
        var migracionPendiente = Crear(idRegistro: null, relato: "Relato sin ID Registro reconocido");
        var fechaIngresadaPorElAdministrador = new DateOnly(2022, 6, 15);

        var informe = migracionPendiente.CrearInformeMigrado(fechaIngresadaPorElAdministrador, idRegistro: "900/2022");

        Assert.Equal("900/2022", informe.IdRegistro);
        Assert.Equal(fechaIngresadaPorElAdministrador, informe.FechaAnalisis);
        Assert.Equal(OrigenInforme.Migrado, informe.Origen);
        Assert.Equal("Relato sin ID Registro reconocido", informe.Relato);
    }

    [Fact]
    public void CompletarIdRegistro_ConMigracionPendienteQueYaTeniaIdRegistro_IgnoraElParametroYUsaElOriginal()
    {
        // Si la MigracionPendiente ya traía IdRegistro (por ejemplo, faltaba
        // solo la Fecha de Análisis), no se pisa con lo que venga en el
        // parámetro — el diseño dice "el Command puede venir con
        // IdRegistro = null (no se pisa el original) o con el mismo valor".
        var migracionPendiente = Crear(idRegistro: "700/2022");
        var fechaIngresadaPorElAdministrador = new DateOnly(2022, 6, 15);

        var informe = migracionPendiente.CrearInformeMigrado(fechaIngresadaPorElAdministrador, idRegistro: null);

        Assert.Equal("700/2022", informe.IdRegistro);
    }
}
