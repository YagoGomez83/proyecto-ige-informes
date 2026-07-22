using IGE.Informes.Application.Common.Interfaces;

namespace IGE.Informes.UnitTests.TestDoubles;

public sealed class FakeInformePdfParser(InformeExtraidoDto resultado) : IInformePdfParser
{
    public InformeExtraidoDto Parsear(Stream pdfStream) => resultado;
}
