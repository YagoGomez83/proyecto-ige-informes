namespace IGE.Informes.Domain.Entities;

/// <summary>
/// Catálogo preseteado de carátulas de Causa (ej. "AV. ROBO", "AV. HURTO
/// CALIFICADO") — evita que la Carátula se tipee como texto libre en cada
/// edición de Informe, con las inconsistencias que eso genera. No
/// reemplaza a Causa: TipoCausa es el catálogo de nombres reutilizables,
/// Causa sigue siendo el expediente puntual (carátula + N° de pieza
/// sumarial + circunscripción judicial) ligado a un Informe concreto — la
/// Carátula de una Causa nueva se elige de este catálogo, o se agrega acá
/// primero si no existe (ver docs/03-modelo-dominio.md).
/// </summary>
public sealed class TipoCausa : IAuditable
{
    public Guid Id { get; private set; }
    public string Nombre { get; private set; } = string.Empty;

    private TipoCausa()
    {
    }

    public TipoCausa(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new ArgumentException("El nombre del Tipo de Causa es obligatorio.", nameof(nombre));
        }

        Id = Guid.NewGuid();
        Nombre = nombre;
    }
}
