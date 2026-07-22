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
}
