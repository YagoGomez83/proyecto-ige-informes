namespace IGE.Informes.Domain.Entities;

/// <summary>
/// Catálogo preseteado de marcas de Vehículo (ej. "Ford", "Fiat",
/// "Volkswagen") — evita que la Marca se tipee como texto libre en cada
/// alta, con las inconsistencias que eso genera (ej. "VW"/"VOLKSWAGEN"/
/// "VOLKWAGEN" para la misma marca real). No reemplaza el campo
/// `Vehiculo.Marca`, que sigue siendo `string`: este catálogo solo
/// alimenta el `<select>` de la UI, con opción de tipear libremente si la
/// marca real no está (ver docs/03-modelo-dominio.md, HU-20).
/// </summary>
public sealed class MarcaVehiculo : IAuditable
{
    public Guid Id { get; private set; }
    public string Nombre { get; private set; } = string.Empty;

    private MarcaVehiculo()
    {
    }

    public MarcaVehiculo(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new ArgumentException("El nombre de la Marca es obligatorio.", nameof(nombre));
        }

        Id = Guid.NewGuid();
        Nombre = nombre;
    }
}
