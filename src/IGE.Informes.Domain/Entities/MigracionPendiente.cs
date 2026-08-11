namespace IGE.Informes.Domain.Entities;

/// <summary>
/// PDF histórico subido en una migración masiva (HU-04) al que le falta
/// el ID Registro y/o la Fecha de Análisis para crear el Informe — se
/// guarda el PDF (en MinIO, ver PdfPath) junto con los demás datos ya
/// extraídos por el parser, para que el Administrador complete lo que
/// falte después sin volver a subir el archivo (docs/03-modelo-dominio.md,
/// "Decisiones ya resueltas"). Entidad transitoria: al completarse los
/// datos faltantes se crea el Informe real vía
/// <see cref="CrearInformeMigrado"/> y esta entidad se borra — no es un
/// estado de Informe, es un paso previo a que exista.
/// </summary>
public sealed class MigracionPendiente : IAuditable
{
    public Guid Id { get; private set; }
    public string? IdRegistro { get; private set; }
    public string PdfPath { get; private set; } = string.Empty;
    public Guid DependenciaDestinoId { get; private set; }
    public Guid UsuarioMigradorId { get; private set; }
    public string? CausaCaratula { get; private set; }
    public string? PiezaSumarial { get; private set; }
    public string? Relato { get; private set; }

    private MigracionPendiente()
    {
    }

    public MigracionPendiente(
        string? idRegistro,
        string pdfPath,
        Guid dependenciaDestinoId,
        Guid usuarioMigradorId,
        string? causaCaratula = null,
        string? piezaSumarial = null,
        string? relato = null)
    {
        // "" o "   " siguen siendo un dato inválido (el parser nunca
        // extraería eso) — null es distinto: un valor válido y explícito
        // que significa "no se reconoció ID Registro".
        if (idRegistro is not null && string.IsNullOrWhiteSpace(idRegistro))
        {
            throw new ArgumentException("El ID Registro no puede ser una cadena vacía — usar null si no se reconoció.", nameof(idRegistro));
        }

        if (string.IsNullOrWhiteSpace(pdfPath))
        {
            throw new ArgumentException("La ruta del PDF en MinIO es obligatoria.", nameof(pdfPath));
        }

        if (dependenciaDestinoId == Guid.Empty)
        {
            throw new ArgumentException("La Dependencia destino es obligatoria.", nameof(dependenciaDestinoId));
        }

        if (usuarioMigradorId == Guid.Empty)
        {
            throw new ArgumentException("El usuario que ejecutó la migración es obligatorio.", nameof(usuarioMigradorId));
        }

        Id = Guid.NewGuid();
        IdRegistro = idRegistro;
        PdfPath = pdfPath;
        DependenciaDestinoId = dependenciaDestinoId;
        UsuarioMigradorId = usuarioMigradorId;
        CausaCaratula = causaCaratula;
        PiezaSumarial = piezaSumarial;
        Relato = relato;
    }

    /// <summary>
    /// Crea el Informe real con los datos ya guardados más la Fecha de
    /// Análisis y, si esta entidad no tenía ID Registro reconocido, el
    /// que ingresó el Administrador (<paramref name="idRegistro"/> — se
    /// ignora si esta entidad ya tenía uno propio, no lo pisa). No borra
    /// esta entidad — eso es responsabilidad del Handler que orquesta la
    /// transacción (crear el Informe + eliminar la MigracionPendiente).
    /// </summary>
    public Informe CrearInformeMigrado(DateOnly fechaAnalisis, string? idRegistro = null, Guid? causaId = null)
    {
        var idRegistroFinal = IdRegistro ?? idRegistro
            ?? throw new InvalidOperationException("Esta Migración Pendiente no tiene ID Registro — hay que informarlo para crear el Informe.");

        var informe = Informe.CrearMigrado(idRegistroFinal, fechaAnalisis, DependenciaDestinoId, UsuarioMigradorId, causaId);
        informe.AsignarPdf(PdfPath);

        if (!string.IsNullOrWhiteSpace(Relato))
        {
            informe.CompletarRelato(Relato);
        }

        return informe;
    }
}
