using IGE.Informes.Domain.Entities;

namespace IGE.Informes.UnitTests.Domain;

public class AlertaTests
{
    [Fact]
    public void PorReincidencia_con_vehiculo_asigna_id_tipo_e_informe_previo()
    {
        var vehiculoId = Guid.NewGuid();
        var informeId = Guid.NewGuid();
        var informePrevioId = Guid.NewGuid();

        var alerta = Alerta.PorReincidencia(vehiculoId, personaId: null, informeId, informePrevioId);

        Assert.NotEqual(Guid.Empty, alerta.Id);
        Assert.Equal(TipoAlerta.ReincidenciaOtroInforme, alerta.Tipo);
        Assert.Equal(vehiculoId, alerta.VehiculoId);
        Assert.Null(alerta.PersonaId);
        Assert.Equal(informeId, alerta.InformeId);
        Assert.Equal(informePrevioId, alerta.InformePrevioId);
        Assert.False(alerta.Atendida);
    }

    [Fact]
    public void PorCargaHuerfana_con_persona_no_tiene_informe_previo()
    {
        var personaId = Guid.NewGuid();
        var informeId = Guid.NewGuid();

        var alerta = Alerta.PorCargaHuerfana(vehiculoId: null, personaId, informeId);

        Assert.Equal(TipoAlerta.CargaHuerfana, alerta.Tipo);
        Assert.Equal(personaId, alerta.PersonaId);
        Assert.Null(alerta.VehiculoId);
        Assert.Null(alerta.InformePrevioId);
    }

    [Fact]
    public void Rechaza_ambos_vehiculo_y_persona_nulos()
    {
        Assert.Throws<ArgumentException>(() =>
            Alerta.PorCargaHuerfana(vehiculoId: null, personaId: null, Guid.NewGuid()));
    }

    [Fact]
    public void Rechaza_ambos_vehiculo_y_persona_presentes()
    {
        Assert.Throws<ArgumentException>(() =>
            Alerta.PorCargaHuerfana(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()));
    }

    [Fact]
    public void Rechaza_informe_vacio()
    {
        Assert.Throws<ArgumentException>(() =>
            Alerta.PorCargaHuerfana(Guid.NewGuid(), personaId: null, Guid.Empty));
    }

    [Fact]
    public void PorReincidencia_rechaza_informe_previo_vacio()
    {
        Assert.Throws<ArgumentException>(() =>
            Alerta.PorReincidencia(Guid.NewGuid(), personaId: null, Guid.NewGuid(), Guid.Empty));
    }

    [Fact]
    public void MarcarAtendida_asigna_usuario_y_fecha()
    {
        var alerta = Alerta.PorCargaHuerfana(Guid.NewGuid(), personaId: null, Guid.NewGuid());
        var usuarioId = Guid.NewGuid();

        alerta.MarcarAtendida(usuarioId);

        Assert.True(alerta.Atendida);
        Assert.Equal(usuarioId, alerta.AtendidaPorUsuarioId);
        Assert.NotNull(alerta.FechaAtencion);
    }

    [Fact]
    public void MarcarAtendida_es_idempotente_no_pisa_la_fecha_de_atencion_original()
    {
        var alerta = Alerta.PorCargaHuerfana(Guid.NewGuid(), personaId: null, Guid.NewGuid());
        var primerUsuario = Guid.NewGuid();
        alerta.MarcarAtendida(primerUsuario);
        var fechaOriginal = alerta.FechaAtencion;

        alerta.MarcarAtendida(Guid.NewGuid());

        Assert.Equal(primerUsuario, alerta.AtendidaPorUsuarioId);
        Assert.Equal(fechaOriginal, alerta.FechaAtencion);
    }
}
