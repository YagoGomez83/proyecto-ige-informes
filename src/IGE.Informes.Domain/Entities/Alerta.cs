namespace IGE.Informes.Domain.Entities;

public enum TipoAlerta
{
    ReincidenciaOtroInforme,
    CargaHuerfana,
}

public sealed class Alerta : IAuditable
{
    public Guid Id { get; private set; }
    public TipoAlerta Tipo { get; private set; }
    public Guid? VehiculoId { get; private set; }
    public Guid? PersonaId { get; private set; }
    public Guid InformeId { get; private set; }
    public Guid? InformePrevioId { get; private set; }
    public DateTime FechaGeneracion { get; private set; }
    public bool Atendida { get; private set; }
    public Guid? AtendidaPorUsuarioId { get; private set; }
    public DateTime? FechaAtencion { get; private set; }

    private Alerta()
    {
    }

    private Alerta(TipoAlerta tipo, Guid? vehiculoId, Guid? personaId, Guid informeId, Guid? informePrevioId)
    {
        if (vehiculoId is null == personaId is null)
        {
            throw new ArgumentException("Una Alerta debe tener exactamente un Vehículo o una Persona, no ambos ni ninguno.");
        }

        if (informeId == Guid.Empty)
        {
            throw new ArgumentException("El Informe que disparó la alerta es obligatorio.", nameof(informeId));
        }

        if (tipo == TipoAlerta.ReincidenciaOtroInforme && (informePrevioId is null || informePrevioId == Guid.Empty))
        {
            throw new ArgumentException(
                "Una Alerta de Reincidencia debe indicar el Informe previo.", nameof(informePrevioId));
        }

        Id = Guid.NewGuid();
        Tipo = tipo;
        VehiculoId = vehiculoId;
        PersonaId = personaId;
        InformeId = informeId;
        InformePrevioId = informePrevioId;
        FechaGeneracion = DateTime.UtcNow;
        Atendida = false;
    }

    public static Alerta PorReincidencia(Guid? vehiculoId, Guid? personaId, Guid informeId, Guid informePrevioId) =>
        new(TipoAlerta.ReincidenciaOtroInforme, vehiculoId, personaId, informeId, informePrevioId);

    public static Alerta PorCargaHuerfana(Guid? vehiculoId, Guid? personaId, Guid informeId) =>
        new(TipoAlerta.CargaHuerfana, vehiculoId, personaId, informeId, informePrevioId: null);

    public void MarcarAtendida(Guid usuarioId)
    {
        if (Atendida)
        {
            return;
        }

        Atendida = true;
        AtendidaPorUsuarioId = usuarioId;
        FechaAtencion = DateTime.UtcNow;
    }
}
