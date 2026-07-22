using IGE.Informes.Application.Common.Interfaces;

namespace IGE.Informes.Infrastructure.PdfParsing;

/// <summary>
/// Adapta el parser estático (InformePdfParser) al puerto IInformePdfParser
/// de Application — Application no puede depender de PdfPig directamente.
/// </summary>
public sealed class InformePdfParserAdapter : IInformePdfParser
{
    public InformeExtraidoDto Parsear(Stream pdfStream)
    {
        var resultado = InformePdfParser.Parsear(pdfStream);

        return new InformeExtraidoDto(
            resultado.IdRegistro,
            resultado.FechaAnalisis,
            resultado.CausaCaratula,
            resultado.Destino,
            resultado.PiezaSumarial,
            resultado.Relato,
            resultado.Vehiculos
                .Select(v => new VehiculoExtraidoDto(v.Marca, v.Modelo, v.Dominio, v.DominioOriginal))
                .ToList(),
            resultado.Personas
                .Select(p => new PersonaExtraidaDto(p.Dni, p.RolSugerido))
                .ToList(),
            resultado.Evidencias
                .Select(e => new EvidenciaExtraidaDto(e.NumeroImagen, e.CodigoCamara, e.Ubicacion, e.FechaHoraCaptura, e.Descripcion))
                .ToList());
    }
}
