using System.Globalization;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;

namespace IGE.Informes.Infrastructure.PdfParsing;

/// <summary>
/// Extrae metadatos de un PDF de "Informe Especial - Análisis de Cámaras
/// de Videovigilancia" por lectura de texto + patrones (ver ADR-004 — sin
/// OCR ni ML, la plantilla es estable y el texto es seleccionable). Reglas
/// tomadas del skill pdf-informe-parser, basado en el análisis de 3
/// informes reales.
/// </summary>
public static partial class InformePdfParser
{
    public static InformeExtraidoResult Parsear(Stream pdfStream)
    {
        using var documento = PdfDocument.Open(pdfStream);
        var texto = string.Join("\n", documento.GetPages().Select(p => p.Text));
        var textoNormalizado = NormalizarTexto(texto);

        var idRegistro = ExtraerIdRegistro(textoNormalizado);
        var fechaAnalisis = ExtraerFechaAnalisis(textoNormalizado);
        var causaCaratula = ExtraerCausaCaratula(textoNormalizado);
        var destino = ExtraerDestino(textoNormalizado);
        var piezaSumarial = ExtraerPiezaSumarial(textoNormalizado);
        var relato = ExtraerRelato(textoNormalizado);
        var vehiculos = ExtraerVehiculos(relato ?? textoNormalizado);
        var personas = ExtraerPersonas(relato ?? textoNormalizado);
        var evidencias = ExtraerEvidencias(textoNormalizado);

        return new InformeExtraidoResult(
            idRegistro,
            fechaAnalisis,
            causaCaratula,
            destino,
            piezaSumarial,
            relato,
            vehiculos,
            personas,
            evidencias);
    }

    /// <summary>
    /// Normaliza espacios/saltos de línea y comillas tipográficas antes de
    /// aplicar cualquier regex — el PDF puede partir un campo en 2 líneas
    /// (ver skill: "Destino... puede partirse en 2 líneas").
    /// </summary>
    private static string NormalizarTexto(string texto)
    {
        var normalizado = texto
            .Replace('“', '"')
            .Replace('”', '"')
            .Replace("\r\n", "\n")
            .Replace('\r', '\n');

        return EspaciosMultiplesRegex().Replace(normalizado, " ");
    }

    private static string? ExtraerIdRegistro(string texto)
    {
        var match = IdRegistroRegex().Match(texto);
        return match.Success ? match.Groups[1].Value : null;
    }

    private static DateOnly? ExtraerFechaAnalisis(string texto)
    {
        var match = FechaAnalisisRegex().Match(texto);
        if (!match.Success)
        {
            return null;
        }

        return ParsearFechaEnEspanol(match.Groups[1].Value.Trim());
    }

    private static DateOnly? ParsearFechaEnEspanol(string fechaTexto)
    {
        var match = FechaTextualRegex().Match(fechaTexto);
        if (!match.Success)
        {
            return null;
        }

        var dia = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        var mesTexto = match.Groups[2].Value.ToUpperInvariant();
        var anio = int.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);

        if (!MesesEnEspanol.TryGetValue(mesTexto, out var mes))
        {
            return null;
        }

        try
        {
            return new DateOnly(anio, mes, dia);
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
    }

    private static readonly IReadOnlyDictionary<string, int> MesesEnEspanol = new Dictionary<string, int>
    {
        ["ENERO"] = 1,
        ["FEBRERO"] = 2,
        ["MARZO"] = 3,
        ["ABRIL"] = 4,
        ["MAYO"] = 5,
        ["JUNIO"] = 6,
        ["JULIO"] = 7,
        ["AGOSTO"] = 8,
        ["SEPTIEMBRE"] = 9,
        ["OCTUBRE"] = 10,
        ["NOVIEMBRE"] = 11,
        ["DICIEMBRE"] = 12,
    };

    private static string? ExtraerCausaCaratula(string texto)
    {
        var match = CausaRegex().Match(texto);
        return match.Success ? match.Groups[1].Value.Trim().Trim('"') : null;
    }

    private static string? ExtraerDestino(string texto)
    {
        var match = DestinoRegex().Match(texto);
        return match.Success ? match.Groups[1].Value.Trim() : null;
    }

    private static string? ExtraerPiezaSumarial(string texto)
    {
        var match = PiezaSumarialRegex().Match(texto);
        return match.Success ? match.Groups[1].Value.Trim() : null;
    }

    /// <summary>
    /// El relato empieza después de "ID REGISTRO:" y termina antes del
    /// primer bloque de Evidencia ("IMAGEN N°..."), o al final del texto
    /// si no hay evidencias reconocibles.
    /// </summary>
    private static string? ExtraerRelato(string texto)
    {
        var inicioMatch = IdRegistroRegex().Match(texto);
        if (!inicioMatch.Success)
        {
            return null;
        }

        var inicio = inicioMatch.Index + inicioMatch.Length;
        var finMatch = ImagenRegex().Match(texto, inicio);
        var fin = finMatch.Success ? finMatch.Index : texto.Length;

        if (fin <= inicio)
        {
            return null;
        }

        var relato = texto[inicio..fin].Trim();
        return string.IsNullOrWhiteSpace(relato) ? null : relato;
    }

    private static IReadOnlyCollection<VehiculoExtraido> ExtraerVehiculos(string texto)
    {
        var resultado = new List<VehiculoExtraido>();

        foreach (Match match in VehiculoRegex().Matches(texto))
        {
            var marca = match.Groups["marca"].Value.Trim();
            var modelo = match.Groups["modelo"].Value.Trim();
            var dominioOriginal = match.Groups["dominio"].Success ? match.Groups["dominio"].Value.Trim() : null;
            var dominioNormalizado = dominioOriginal?.Replace(" ", "").Replace("-", "");

            resultado.Add(new VehiculoExtraido(marca, modelo, dominioNormalizado, dominioOriginal ?? "no visible"));
        }

        return resultado;
    }

    private static IReadOnlyCollection<PersonaExtraida> ExtraerPersonas(string texto)
    {
        var resultado = new List<PersonaExtraida>();

        foreach (Match match in DniRegex().Matches(texto))
        {
            var dni = match.Groups[1].Value.Replace(".", "");
            var contexto = ObtenerContextoAlrededor(texto, match.Index, 60);
            var rolSugerido = InferirRolPersona(contexto);

            resultado.Add(new PersonaExtraida(dni, rolSugerido));
        }

        return resultado;
    }

    private static string ObtenerContextoAlrededor(string texto, int posicion, int radio)
    {
        var inicio = Math.Max(0, posicion - radio);
        var fin = Math.Min(texto.Length, posicion + radio);
        return texto[inicio..fin];
    }

    private static string? InferirRolPersona(string contexto)
    {
        var contextoUpper = contexto.ToUpperInvariant();

        if (contextoUpper.Contains("DAMNIFICAD"))
        {
            return "Damnificado";
        }

        if (contextoUpper.Contains("DENUNCIANT"))
        {
            return "Denunciante";
        }

        if (contextoUpper.Contains("SOSPECHOS"))
        {
            return "Sospechoso";
        }

        if (contextoUpper.Contains("TESTIGO"))
        {
            return "Testigo";
        }

        return null;
    }

    private static IReadOnlyCollection<EvidenciaExtraida> ExtraerEvidencias(string texto)
    {
        var resultado = new List<EvidenciaExtraida>();

        foreach (Match match in ImagenRegex().Matches(texto))
        {
            // "IMAGEN N° 15 Y N° 16" son dos evidencias separadas aunque
            // compartan el mismo párrafo descriptivo (ver skill).
            var numeros = NumerosEnTituloRegex().Matches(match.Value)
                .Select(m => int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture))
                .Distinct()
                .ToList();

            var finDescripcion = ImagenRegex().Match(texto, match.Index + match.Length);
            var finPos = finDescripcion.Success ? finDescripcion.Index : texto.Length;
            var descripcion = texto[(match.Index + match.Length)..finPos].Trim();

            var (codigoCamara, ubicacion, fechaHora) = ExtraerDatosCaptura(descripcion);

            foreach (var numero in numeros)
            {
                resultado.Add(new EvidenciaExtraida(numero, codigoCamara, ubicacion, fechaHora, descripcion));
            }
        }

        return resultado;
    }

    /// <summary>
    /// Título de captura formato Domo: "&lt;CÓDIGO&gt; - &lt;ubicación&gt; -
    /// &lt;fecha&gt; &lt;hora&gt;", o formato LPR: "Location:.../Capture
    /// Time:...". Si no matchea ninguno, se deja sin resolver — no bloquea
    /// la carga (ver skill).
    /// </summary>
    private static (string? Codigo, string? Ubicacion, DateTime? FechaHora) ExtraerDatosCaptura(string descripcion)
    {
        var domoMatch = TituloCapturaDomoRegex().Match(descripcion);
        if (domoMatch.Success)
        {
            var codigo = domoMatch.Groups["codigo"].Value.Trim();
            var ubicacion = domoMatch.Groups["ubicacion"].Value.Trim();
            var fechaHoraTexto = domoMatch.Groups["fechahora"].Value.Trim();

            var fechaHora = ParsearFechaHoraComoUtc(fechaHoraTexto);

            return (codigo, ubicacion, fechaHora);
        }

        var lprUbicacion = LprLocationRegex().Match(descripcion);
        var lprFecha = LprCaptureTimeRegex().Match(descripcion);

        if (lprUbicacion.Success || lprFecha.Success)
        {
            var fechaHora = lprFecha.Success ? ParsearFechaHoraComoUtc(lprFecha.Groups[1].Value.Trim()) : null;

            return (null, lprUbicacion.Success ? lprUbicacion.Groups[1].Value.Trim() : null, fechaHora);
        }

        return (null, null, null);
    }

    /// <summary>
    /// El PDF no indica zona horaria para la fecha/hora de captura de una
    /// cámara — se interpreta como hora local de Argentina y se fija
    /// explícitamente Kind=Utc antes de exponerla, porque Npgsql rechaza
    /// persistir un DateTime con Kind=Unspecified en una columna
    /// "timestamp with time zone".
    /// </summary>
    private static DateTime? ParsearFechaHoraComoUtc(string fechaHoraTexto)
    {
        if (!DateTime.TryParse(fechaHoraTexto, CultureInfo.GetCultureInfo("es-AR"), DateTimeStyles.None, out var fecha))
        {
            return null;
        }

        return DateTime.SpecifyKind(fecha, DateTimeKind.Utc);
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex EspaciosMultiplesRegex();

    [GeneratedRegex(@"ID\s*REGISTRO\s*:\s*(\d+/\d{4})", RegexOptions.IgnoreCase)]
    private static partial Regex IdRegistroRegex();

    [GeneratedRegex(@"FECHA\s*DE\s*AN[ÁA]LISIS\s*:\s*([^:]+?)(?=\s*CAUSA\s*:|\s*DESTINO\s*:|$)", RegexOptions.IgnoreCase)]
    private static partial Regex FechaAnalisisRegex();

    [GeneratedRegex(@"(\d{1,2})\s*DE\s*([A-ZÁÉÍÓÚ]+)\s*DE\s*(\d{4})", RegexOptions.IgnoreCase)]
    private static partial Regex FechaTextualRegex();

    [GeneratedRegex(@"CAUSA\s*:\s*[""']?([^""'\n]+?)[""']?\s*(?=DESTINO\s*:|ELEVA\s*:|ID\s*REGISTRO\s*:|$)", RegexOptions.IgnoreCase)]
    private static partial Regex CausaRegex();

    [GeneratedRegex(@"DESTINO\s*:\s*(.+?)\s*(?=ELEVA\s*:|ID\s*REGISTRO\s*:|$)", RegexOptions.IgnoreCase)]
    private static partial Regex DestinoRegex();

    [GeneratedRegex(@"PIEZA\s*SUMARIAL\s*N?°?\s*:?\s*([\d/]+)", RegexOptions.IgnoreCase)]
    private static partial Regex PiezaSumarialRegex();

    [GeneratedRegex(@"MARCA\s+(?<marca>\w+)[,.]?\s*MODELO\s+(?<modelo>[\w\s]+?)(?=\s*(?:,|\.|DOMINIO|$))\s*(?:DOMINIO\s*(?:VISIBLE)?\s*:?\s*(?<dominio>[A-Z]{2,3}[\s-]?\d{2,3})|DOMINIO\s+NO\s+VISIBLE)?", RegexOptions.IgnoreCase)]
    private static partial Regex VehiculoRegex();

    [GeneratedRegex(@"DNI\s*N?°?:?\s*([\d.]+)", RegexOptions.IgnoreCase)]
    private static partial Regex DniRegex();

    [GeneratedRegex(@"IMAGEN\s*N°\s*(\d+(?:\s*Y\s*N°\s*\d+)*)\s*[–-]\s*", RegexOptions.IgnoreCase)]
    private static partial Regex ImagenRegex();

    [GeneratedRegex(@"(\d+)")]
    private static partial Regex NumerosEnTituloRegex();

    [GeneratedRegex(@"(?<codigo>[A-Z]{2}\s?\d{1,3})\s*-\s*(?<ubicacion>[^-]+)-\s*(?<fechahora>\d{1,2}/\d{1,2}/\d{4}\s+\d{1,2}:\d{2}:\d{2}(?:\.\d+)?)", RegexOptions.IgnoreCase)]
    private static partial Regex TituloCapturaDomoRegex();

    [GeneratedRegex(@"Location\s*:\s*([^\n]+)", RegexOptions.IgnoreCase)]
    private static partial Regex LprLocationRegex();

    [GeneratedRegex(@"Capture\s*Time\s*:\s*([^\n]+)", RegexOptions.IgnoreCase)]
    private static partial Regex LprCaptureTimeRegex();
}
