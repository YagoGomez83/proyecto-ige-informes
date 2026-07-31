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

    [Fact]
    public void Marcas_de_paginacion_no_quedan_intercaladas_en_el_relato()
    {
        // PdfPig concatena el texto de todas las páginas sin distinguir
        // pie/encabezado del contenido real — "Página X de Y" se repite en
        // cada hoja y debe filtrarse antes de guardar el Relato (informe
        // real 95/2022, ver memoria del proyecto).
        using var pdf = GeneradorPdfDePrueba.GenerarPdf([
            "ID REGISTRO: 95/2022",
            "Página 2 de 10 Atento a nota recepcionada en éste centro,",
            "la instrucción solicita se realice un informe. Página 3 de 10",
            "IMÁGENES CAMARAS DE MONITOREO Imagen 1 – Se observa un vehículo.",
        ]);

        var resultado = InformePdfParser.Parsear(pdf);

        Assert.NotNull(resultado.Relato);
        Assert.DoesNotContain("Página", resultado.Relato);
        Assert.Contains("Atento a nota recepcionada", resultado.Relato);
    }

    [Fact]
    public void Paginacion_con_separador_distinto_a_la_palabra_de_tambien_se_filtra()
    {
        // Variante real observada: PdfPig extrae un glifo distinto en vez
        // de la palabra "de" entre los dos números de página (ver informes
        // reales 79-84/2022 y 88/2022, ver memoria del proyecto).
        using var pdf = GeneradorPdfDePrueba.GenerarPdf([
            "ID REGISTRO: 81/2022",
            "Página 2 | 25 Atento a nota recepcionada se solicita un informe.",
        ]);

        var resultado = InformePdfParser.Parsear(pdf);

        Assert.NotNull(resultado.Relato);
        Assert.DoesNotContain("Página", resultado.Relato);
        Assert.Contains("Atento a nota recepcionada", resultado.Relato);
    }

    [Fact]
    public void LimpiarRelatoExistente_saca_paginacion_y_corta_en_la_primera_imagen()
    {
        // Caso real (Informe 95/2022 migrado sin PDF original, no se puede
        // re-parsear desde el archivo — ver subcomando "limpiar-relatos" de
        // IGE.Informes.DataMigration) — el Relato ya persistido tenía el
        // documento completo por el bug ya corregido del parser.
        var relatoContaminado =
            "Página 2 de 10 Atento a nota recepcionada se solicita un informe especial. " +
            "Página 3 de 10 IMÁGENES CAMARAS DE MONITOREO Imagen 1 – Se observa un vehículo.";

        var limpio = InformePdfParser.LimpiarRelatoExistente(relatoContaminado);

        Assert.NotNull(limpio);
        Assert.DoesNotContain("Página", limpio);
        Assert.DoesNotContain("Imagen 1", limpio);
        Assert.Contains("Atento a nota recepcionada", limpio);
    }

    [Fact]
    public void LimpiarRelatoExistente_devuelve_null_si_no_queda_texto_tras_limpiar()
    {
        var limpio = InformePdfParser.LimpiarRelatoExistente("Página 2 de 10   ");

        Assert.Null(limpio);
    }

    [Fact]
    public void Formato_imagen_sin_simbolo_de_grado_tambien_corta_el_relato_y_genera_evidencia()
    {
        // Variante real del formato de encabezado de imagen: "Imagen 1 –"
        // en vez de "IMAGEN N° 1 –" (informe real 95/2022) — el parser
        // original solo reconocía la segunda forma, y sin match el Relato
        // se quedaba con el documento entero (evidencias, datos del
        // automotor, firmantes).
        using var pdf = GeneradorPdfDePrueba.GenerarPdf([
            "ID REGISTRO: 95/2022",
            "Atento a nota recepcionada se solicita un informe especial.",
            "Imagen 1 – Se observa un vehículo símil características circulando.",
        ]);

        var resultado = InformePdfParser.Parsear(pdf);

        Assert.Equal("Atento a nota recepcionada se solicita un informe especial.", resultado.Relato);
        var evidencia = Assert.Single(resultado.Evidencias);
        Assert.Equal(1, evidencia.NumeroImagen);
    }
}
