using IGE.Informes.Infrastructure.PdfParsing;

namespace IGE.Informes.UnitTests.PdfParsing;

public class InformePdfParserTests
{
    [Fact]
    public void Extrae_el_encabezado_completo()
    {
        using var pdf = GeneradorPdfDePrueba.GenerarPdf([
            "FECHA DE ANÁLISIS: 14 DE JULIO DE 2026",
            "CAUSA: “AV. INFRACCION LEY 23.737”",
            "DESTINO: DIVISION LUCHA CONTRA EL",
            "NARCO TRAFICO DG-8",
            "ELEVA: INSTITUTO DE GESTIÓN DE EMERGENCIAS 4.0",
            "ID REGISTRO: 08/2026",
            "PIEZA SUMARIAL N° 7070029/26",
            "En el presente informe se procede a realizar el análisis solicitado.",
        ]);

        var resultado = InformePdfParser.Parsear(pdf);

        Assert.Equal("08/2026", resultado.IdRegistro);
        Assert.Equal(new DateOnly(2026, 7, 14), resultado.FechaAnalisis);
        Assert.Equal("AV. INFRACCION LEY 23.737", resultado.CausaCaratula);
        Assert.Contains("DIVISION LUCHA CONTRA EL", resultado.Destino);
        Assert.Contains("NARCO TRAFICO DG-8", resultado.Destino);
        Assert.Equal("7070029/26", resultado.PiezaSumarial);
        Assert.False(resultado.RequiereRevisionManual);
    }

    [Fact]
    public void Tolera_lineas_sin_separador_entre_campos()
    {
        // PdfPig concatena el texto de líneas consecutivas sin espacio ni
        // salto cuando no hay uno explícito en el contenido — el parser
        // debe reconocer los campos igual, no solo cuando hay espacio.
        using var pdf = GeneradorPdfDePrueba.GenerarPdf(
            ["FECHA DE ANÁLISIS: 14 DE JULIO DE 2026CAUSA: “AV. INFRACCION LEY 23.737”ID REGISTRO: 08/2026"]);

        var resultado = InformePdfParser.Parsear(pdf);

        Assert.Equal("08/2026", resultado.IdRegistro);
        Assert.Equal(new DateOnly(2026, 7, 14), resultado.FechaAnalisis);
        Assert.Equal("AV. INFRACCION LEY 23.737", resultado.CausaCaratula);
    }

    [Fact]
    public void IdRegistro_no_reconocido_marca_para_revision_manual()
    {
        using var pdf = GeneradorPdfDePrueba.GenerarPdf([
            "FECHA DE ANÁLISIS: 14 DE JULIO DE 2026",
            "CAUSA: “AV. INFRACCION LEY 23.737”",
        ]);

        var resultado = InformePdfParser.Parsear(pdf);

        Assert.Null(resultado.IdRegistro);
        Assert.True(resultado.RequiereRevisionManual);
    }

    [Fact]
    public void Informe_sin_vehiculo_solo_persona_no_devuelve_vehiculos()
    {
        using var pdf = GeneradorPdfDePrueba.GenerarPdf([
            "ID REGISTRO: 290/2026",
            "En el marco de la investigación se identificó a la denunciante",
            "DNI N° 30.123.456, quien manifestó haber sido víctima de hurto.",
            "IMAGEN N° 1 – Captura del comercio",
        ]);

        var resultado = InformePdfParser.Parsear(pdf);

        Assert.Empty(resultado.Vehiculos);
        Assert.Single(resultado.Personas);
        Assert.Equal("30123456", resultado.Personas.Single().Dni);
        Assert.Equal("Denunciante", resultado.Personas.Single().RolSugerido);
    }

    [Fact]
    public void Informe_con_dos_vehiculos_distintos_los_extrae_ambos()
    {
        using var pdf = GeneradorPdfDePrueba.GenerarPdf([
            "ID REGISTRO: 08/2026",
            "Se observa un vehículo marca TOYOTA modelo HILUX dominio IAK 796",
            "circulando por la zona, y posteriormente un vehículo",
            "marca CHEVROLET modelo CELTA dominio visible: JK051",
            "IMAGEN N° 1 – Primera captura",
        ]);

        var resultado = InformePdfParser.Parsear(pdf);

        Assert.Equal(2, resultado.Vehiculos.Count);
        Assert.Contains(resultado.Vehiculos, v => v.Marca == "TOYOTA" && v.Dominio == "IAK796");
        Assert.Contains(resultado.Vehiculos, v => v.Marca == "CHEVROLET" && v.Dominio == "JK051");
    }

    [Fact]
    public void Dominio_no_visible_se_extrae_el_vehiculo_sin_dominio()
    {
        using var pdf = GeneradorPdfDePrueba.GenerarPdf([
            "ID REGISTRO: 293/2026",
            "Se observa una motocicleta marca ZANELLA modelo 150 CC",
            "dominio no visible en la captura.",
            "IMAGEN N° 1 – Captura de la motocicleta",
        ]);

        var resultado = InformePdfParser.Parsear(pdf);

        var vehiculo = Assert.Single(resultado.Vehiculos);
        Assert.Equal("ZANELLA", vehiculo.Marca);
        Assert.Null(vehiculo.Dominio);
        Assert.Equal("no visible", vehiculo.DominioOriginal);
    }

    [Fact]
    public void Evidencias_con_titulo_domo_se_reconoce_camara_ubicacion_y_fecha()
    {
        using var pdf = GeneradorPdfDePrueba.GenerarPdf([
            "ID REGISTRO: 08/2026",
            "IMAGEN N° 1 – SL 18 - Lafinur y Junin - 2/7/2026 20:40:42",
            "Se observa al sujeto ingresando al comercio.",
        ]);

        var resultado = InformePdfParser.Parsear(pdf);

        var evidencia = Assert.Single(resultado.Evidencias);
        Assert.Equal(1, evidencia.NumeroImagen);
        Assert.Equal("SL 18", evidencia.CodigoCamara);
        Assert.Equal("Lafinur y Junin", evidencia.Ubicacion);
        Assert.NotNull(evidencia.FechaHoraCaptura);

        // Npgsql rechaza persistir un DateTime con Kind=Unspecified en una
        // columna "timestamp with time zone" — el PDF no trae zona
        // horaria, así que el parser debe fijar Kind=Utc explícitamente.
        Assert.Equal(DateTimeKind.Utc, evidencia.FechaHoraCaptura!.Value.Kind);
    }

    [Fact]
    public void Imagenes_dobles_en_un_mismo_titulo_generan_dos_evidencias_separadas()
    {
        using var pdf = GeneradorPdfDePrueba.GenerarPdf([
            "ID REGISTRO: 08/2026",
            "IMAGEN N° 15 Y N° 16 – Capturas del mismo recorrido vehicular.",
        ]);

        var resultado = InformePdfParser.Parsear(pdf);

        Assert.Equal(2, resultado.Evidencias.Count);
        Assert.Contains(resultado.Evidencias, e => e.NumeroImagen == 15);
        Assert.Contains(resultado.Evidencias, e => e.NumeroImagen == 16);
    }

    [Fact]
    public void Titulo_de_imagen_no_reconocido_no_bloquea_la_carga()
    {
        using var pdf = GeneradorPdfDePrueba.GenerarPdf([
            "ID REGISTRO: 08/2026",
            "IMAGEN N° 1 – Fotografía sin metadatos de cámara reconocibles.",
        ]);

        var resultado = InformePdfParser.Parsear(pdf);

        var evidencia = Assert.Single(resultado.Evidencias);
        Assert.Null(evidencia.CodigoCamara);
        Assert.NotNull(evidencia.Descripcion);
    }
}
