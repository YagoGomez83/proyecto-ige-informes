using UglyToad.PdfPig.Writer;

namespace IGE.Informes.UnitTests.PdfParsing;

/// <summary>
/// Genera PDFs sintéticos con la estructura del Informe Especial (ver
/// skill pdf-informe-parser) para testear InformePdfParser sin depender
/// de los 3 PDFs reales de muestra (no versionados en el repo por
/// privacidad de datos reales de investigaciones — ver memoria del
/// proyecto, deuda pendiente: validar contra los originales). Usa
/// DejaVu Sans embebida en el repo (TrueType) en vez de una fuente
/// Standard14 — estas últimas no soportan vocales acentuadas en
/// mayúscula (ej. 'Ó') bajo WinAnsi. Se usa un archivo propio en vez de
/// una fuente del sistema operativo para que el test corra igual en
/// Windows (dev local) y Linux (CI en GitHub Actions).
/// </summary>
public static class GeneradorPdfDePrueba
{
    private static readonly string RutaFuente =
        Path.Combine(AppContext.BaseDirectory, "PdfParsing", "Resources", "DejaVuSans.ttf");

    public static MemoryStream GenerarPdf(IReadOnlyList<string> lineas)
    {
        var builder = new PdfDocumentBuilder();
        var fuente = builder.AddTrueTypeFont(File.ReadAllBytes(RutaFuente));
        var pagina = builder.AddPage(595, 842); // A4

        double y = 800;
        const double alturaLinea = 14;
        const double margenIzquierdo = 40;

        foreach (var linea in lineas)
        {
            if (y < 40)
            {
                pagina = builder.AddPage(595, 842);
                y = 800;
            }

            pagina.AddText(linea + " ", 10, new UglyToad.PdfPig.Core.PdfPoint(margenIzquierdo, y), fuente);
            y -= alturaLinea;
        }

        var bytes = builder.Build();
        return new MemoryStream(bytes);
    }
}
