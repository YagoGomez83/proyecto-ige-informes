using IGE.Informes.Domain.Entities;

namespace IGE.Informes.UnitTests.Domain;

public class EvidenciaTests
{
    private static readonly Guid InformeId = Guid.NewGuid();

    [Fact]
    public void Alta_valida_asigna_id_y_datos()
    {
        var evidencia = new Evidencia(1, InformeId, descripcion: "Sujeto ingresando al comercio");

        Assert.NotEqual(Guid.Empty, evidencia.Id);
        Assert.Equal(1, evidencia.NumeroImagen);
        Assert.Equal(InformeId, evidencia.InformeId);
        Assert.Null(evidencia.CamaraId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Alta_rechaza_numero_de_imagen_no_positivo(int numeroImagen)
    {
        Assert.Throws<ArgumentException>(() => new Evidencia(numeroImagen, InformeId));
    }

    [Fact]
    public void Alta_rechaza_informe_vacio()
    {
        Assert.Throws<ArgumentException>(() => new Evidencia(1, Guid.Empty));
    }

    [Fact]
    public void AsignarCamara_completa_una_evidencia_sin_camara_resuelta()
    {
        var evidencia = new Evidencia(1, InformeId);
        var camaraId = Guid.NewGuid();

        evidencia.AsignarCamara(camaraId);

        Assert.Equal(camaraId, evidencia.CamaraId);
    }

    [Fact]
    public void VincularVehiculo_permite_multiples_vehiculos_sin_duplicar()
    {
        var evidencia = new Evidencia(1, InformeId);
        var vehiculoId = Guid.NewGuid();

        evidencia.VincularVehiculo(vehiculoId);
        evidencia.VincularVehiculo(vehiculoId);
        evidencia.VincularVehiculo(Guid.NewGuid());

        Assert.Equal(2, evidencia.VehiculoIds.Count);
    }

    [Fact]
    public void VincularPersona_permite_multiples_personas_sin_duplicar()
    {
        var evidencia = new Evidencia(1, InformeId);
        var personaId = Guid.NewGuid();

        evidencia.VincularPersona(personaId);
        evidencia.VincularPersona(personaId);

        Assert.Single(evidencia.PersonaIds);
    }
}
