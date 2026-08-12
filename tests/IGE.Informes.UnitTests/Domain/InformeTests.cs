using IGE.Informes.Domain.Entities;

namespace IGE.Informes.UnitTests.Domain;

public class InformeTests
{
    private static readonly Guid CasoAnalisisId = Guid.NewGuid();
    private static readonly Guid DependenciaDestinoId = Guid.NewGuid();
    private static readonly Guid AnalistaSolicitanteId = Guid.NewGuid();

    private static Informe CrearInforme(Guid? causaId = null) => new(
        "290/2026",
        new DateOnly(2026, 7, 21),
        CasoAnalisisId,
        DependenciaDestinoId,
        AnalistaSolicitanteId,
        causaId);

    [Fact]
    public void Alta_nace_en_Borrador_y_asigna_al_solicitante_como_Interviniente()
    {
        var informe = CrearInforme();

        Assert.Equal(EstadoInforme.Borrador, informe.Estado);
        Assert.Single(informe.Analistas);
        Assert.Equal(AnalistaSolicitanteId, informe.Analistas.Single().UsuarioId);
        Assert.Equal(RolInformeAnalista.Interviniente, informe.Analistas.Single().Rol);
    }

    [Fact]
    public void Alta_sin_Causa_se_acepta_dado_que_es_nullable()
    {
        var informe = CrearInforme(causaId: null);

        Assert.Null(informe.CausaId);
    }

    [Fact]
    public void Alta_con_Causa_la_vincula()
    {
        var causaId = Guid.NewGuid();

        var informe = CrearInforme(causaId);

        Assert.Equal(causaId, informe.CausaId);
    }

    [Fact]
    public void Alta_normal_tiene_Origen_CargaIndividual_y_CasoAnalisisId()
    {
        var informe = CrearInforme();

        Assert.Equal(OrigenInforme.CargaIndividual, informe.Origen);
        Assert.Equal(CasoAnalisisId, informe.CasoAnalisisId);
    }

    [Fact]
    public void CrearMigrado_nace_sin_CasoAnalisisId_con_Origen_Migrado()
    {
        var usuarioMigradorId = Guid.NewGuid();

        var informe = Informe.CrearMigrado("500/2018", new DateOnly(2018, 3, 10), DependenciaDestinoId, usuarioMigradorId);

        Assert.Null(informe.CasoAnalisisId);
        Assert.Equal(OrigenInforme.Migrado, informe.Origen);
        Assert.Equal(EstadoInforme.Borrador, informe.Estado);
        Assert.Single(informe.Analistas);
        Assert.Equal(usuarioMigradorId, informe.Analistas.Single().UsuarioId);
        Assert.Equal(RolInformeAnalista.Interviniente, informe.Analistas.Single().Rol);
    }

    [Fact]
    public void CrearMigrado_acepta_Causa_opcional()
    {
        var causaId = Guid.NewGuid();

        var informe = Informe.CrearMigrado("500/2018", new DateOnly(2018, 3, 10), DependenciaDestinoId, Guid.NewGuid(), causaId);

        Assert.Equal(causaId, informe.CausaId);
    }

    [Fact]
    public void CrearMigrado_rechaza_dependencia_destino_vacia()
    {
        Assert.Throws<ArgumentException>(() => Informe.CrearMigrado(
            "500/2018", new DateOnly(2018, 3, 10), Guid.Empty, Guid.NewGuid()));
    }

    [Fact]
    public void CrearMigrado_rechaza_usuario_migrador_vacio()
    {
        Assert.Throws<ArgumentException>(() => Informe.CrearMigrado(
            "500/2018", new DateOnly(2018, 3, 10), DependenciaDestinoId, Guid.Empty));
    }

    [Fact]
    public void Alta_rechaza_id_registro_vacio()
    {
        Assert.Throws<ArgumentException>(() => new Informe(
            "", new DateOnly(2026, 7, 21), CasoAnalisisId, DependenciaDestinoId, AnalistaSolicitanteId));
    }

    [Fact]
    public void Alta_rechaza_caso_de_analisis_vacio()
    {
        Assert.Throws<ArgumentException>(() => new Informe(
            "290/2026", new DateOnly(2026, 7, 21), Guid.Empty, DependenciaDestinoId, AnalistaSolicitanteId));
    }

    [Fact]
    public void Alta_rechaza_dependencia_destino_vacia()
    {
        Assert.Throws<ArgumentException>(() => new Informe(
            "290/2026", new DateOnly(2026, 7, 21), CasoAnalisisId, Guid.Empty, AnalistaSolicitanteId));
    }

    [Fact]
    public void CompletarRelato_guarda_el_relato_en_Borrador()
    {
        var informe = CrearInforme();

        informe.CompletarRelato("Se procede a realizar el análisis...");

        Assert.Equal("Se procede a realizar el análisis...", informe.Relato);
    }

    [Fact]
    public void AsignarPdf_guarda_la_ruta()
    {
        var informe = CrearInforme();

        informe.AsignarPdf("informes/290-2026.pdf");

        Assert.Equal("informes/290-2026.pdf", informe.PdfPath);
    }

    [Fact]
    public void Publicar_sin_Causa_lo_rechaza()
    {
        var informe = CrearInforme(causaId: null);
        informe.AgregarFirmante(Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(() => informe.Publicar());
    }

    [Fact]
    public void Publicar_sin_Firmante_lo_rechaza()
    {
        var informe = CrearInforme(Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(() => informe.Publicar());
    }

    [Fact]
    public void Publicar_con_Causa_y_Firmante_pasa_a_Publicado()
    {
        var informe = CrearInforme(Guid.NewGuid());
        informe.AgregarFirmante(Guid.NewGuid());

        informe.Publicar();

        Assert.Equal(EstadoInforme.Publicado, informe.Estado);
    }

    [Fact]
    public void AgregarFirmante_no_duplica_al_mismo_usuario_como_Interviniente_y_Firmante()
    {
        var informe = CrearInforme(Guid.NewGuid());

        informe.AgregarFirmante(AnalistaSolicitanteId);

        Assert.Single(informe.Analistas);
        Assert.Equal(RolInformeAnalista.Firmante, informe.Analistas.Single().Rol);
    }

    [Fact]
    public void Informe_Publicado_es_inmutable_para_Relato()
    {
        var informe = CrearInforme(Guid.NewGuid());
        informe.AgregarFirmante(Guid.NewGuid());
        informe.Publicar();

        Assert.Throws<InvalidOperationException>(() => informe.CompletarRelato("intento de edición"));
    }

    [Fact]
    public void Informe_Publicado_es_inmutable_para_Pdf()
    {
        var informe = CrearInforme(Guid.NewGuid());
        informe.AgregarFirmante(Guid.NewGuid());
        informe.Publicar();

        Assert.Throws<InvalidOperationException>(() => informe.AsignarPdf("otro.pdf"));
    }

    // HU-02 · Editar / corregir metadatos de un informe (Épica 01),
    // escenario "Corregir el ID Registro de un informe en Borrador":
    // mismo patrón que CorregirFechaAnalisis (bloquea si Estado ==
    // Publicado). El chequeo de duplicado contra otros Informes NO es
    // responsabilidad del dominio, vive en EditarInformeCommandHandler (ver
    // EditarInformeCommandHandlerTests, escenario "Rechazar un ID Registro
    // que ya existe en otro informe").

    [Fact]
    public void CorregirIdRegistro_EnBorrador_ActualizaElIdRegistro()
    {
        var informe = CrearInforme();

        informe.CorregirIdRegistro("101/2022");

        Assert.Equal("101/2022", informe.IdRegistro);
    }

    [Fact]
    public void CorregirIdRegistro_ConValorVacio_LoRechaza()
    {
        var informe = CrearInforme();

        Assert.Throws<ArgumentException>(() => informe.CorregirIdRegistro(""));
    }

    [Fact]
    public void CorregirIdRegistro_InformePublicado_EsInmutable()
    {
        var informe = CrearInforme(Guid.NewGuid());
        informe.AgregarFirmante(Guid.NewGuid());
        informe.Publicar();

        Assert.Throws<InvalidOperationException>(() => informe.CorregirIdRegistro("101/2022"));
    }
}
