namespace IGE.Informes.Application.Common.Validation;

/// <summary>
/// Confirma el formato real de una imagen por sus primeros bytes (magic
/// numbers), en vez de confiar en el Content-Type que declara el cliente —
/// ese valor lo controla el navegador/usuario y puede no corresponder al
/// contenido real del archivo (ver docs/06-seguridad-amenazas.md, sección
/// "Ingesta de PDFs", mismo criterio de "tipo MIME real, no solo
/// declarado" ya aplicado ahí).
/// </summary>
public static class FormatoImagenHelper
{
    public static readonly IReadOnlyCollection<string> TiposMimePermitidos = ["image/jpeg", "image/png", "image/webp"];

    /// <summary>
    /// Devuelve true si los primeros bytes del contenido corresponden
    /// exactamente al tipo MIME declarado.
    /// </summary>
    public static bool CoincideConTipoDeclarado(byte[] contenido, string tipoMimeDeclarado) => tipoMimeDeclarado switch
    {
        "image/jpeg" => EsJpeg(contenido),
        "image/png" => EsPng(contenido),
        "image/webp" => EsWebp(contenido),
        _ => false,
    };

    private static bool EsJpeg(byte[] c) =>
        c.Length >= 3 && c[0] == 0xFF && c[1] == 0xD8 && c[2] == 0xFF;

    private static bool EsPng(byte[] c) =>
        c.Length >= 8
        && c[0] == 0x89 && c[1] == 0x50 && c[2] == 0x4E && c[3] == 0x47
        && c[4] == 0x0D && c[5] == 0x0A && c[6] == 0x1A && c[7] == 0x0A;

    private static bool EsWebp(byte[] c) =>
        c.Length >= 12
        && c[0] == 0x52 && c[1] == 0x49 && c[2] == 0x46 && c[3] == 0x46 // "RIFF"
        && c[8] == 0x57 && c[9] == 0x45 && c[10] == 0x42 && c[11] == 0x50; // "WEBP"
}
