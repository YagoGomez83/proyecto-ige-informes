using IGE.Informes.Application.Common.Interfaces;

namespace IGE.Informes.UnitTests.TestDoubles;

/// <summary>
/// Doble de <see cref="IInformePdfParser"/> para HU-04 (migración por lote):
/// a diferencia de <see cref="FakeInformePdfParser"/> (un único resultado
/// fijo, alcanza para HU-01), acá cada PDF del lote necesita un resultado
/// — o una excepción simulando "PDF no legible" — distinto. Se resuelve
/// por nombre de archivo porque <see cref="IInformePdfParser.Parsear"/>
/// solo recibe el stream, así que el Fake intercepta la llamada
/// correspondiente en el orden en que el Handler va abriendo los streams
/// del lote (uno por nombre, en el orden declarado).
/// </summary>
public sealed class FakeInformePdfParserPorArchivo : IInformePdfParser
{
    private readonly Queue<Func<InformeExtraidoDto>> _resultados = new();

    public FakeInformePdfParserPorArchivo ConResultado(InformeExtraidoDto resultado)
    {
        _resultados.Enqueue(() => resultado);
        return this;
    }

    public FakeInformePdfParserPorArchivo ConExcepcion(Exception excepcion)
    {
        _resultados.Enqueue(() => throw excepcion);
        return this;
    }

    /// <summary>
    /// Simula un parseo que nunca termina en un tiempo razonable — para
    /// probar el timeout por archivo de MigrarInformesCommandHandler sin
    /// depender de un PDF real que cuelgue PdfPig.
    /// </summary>
    public FakeInformePdfParserPorArchivo ConDemora(TimeSpan demora, InformeExtraidoDto resultado)
    {
        _resultados.Enqueue(() =>
        {
            Thread.Sleep(demora);
            return resultado;
        });
        return this;
    }

    public InformeExtraidoDto Parsear(Stream pdfStream)
    {
        if (_resultados.Count == 0)
        {
            throw new InvalidOperationException("FakeInformePdfParserPorArchivo: no quedan resultados configurados para esta llamada.");
        }

        return _resultados.Dequeue().Invoke();
    }
}
