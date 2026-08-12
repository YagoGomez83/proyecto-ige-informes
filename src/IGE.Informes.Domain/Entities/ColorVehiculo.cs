namespace IGE.Informes.Domain.Entities;

/// <summary>
/// Catálogo preseteado de colores de Vehículo (ej. "Blanco", "Negro",
/// "Gris Oscuro") — mismo criterio que <see cref="MarcaVehiculo"/> pero
/// para el campo Color. No reemplaza `Vehiculo.Color`, que sigue siendo
/// `string`.
/// </summary>
public sealed class ColorVehiculo : IAuditable
{
    public Guid Id { get; private set; }
    public string Nombre { get; private set; } = string.Empty;

    private ColorVehiculo()
    {
    }

    public ColorVehiculo(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new ArgumentException("El nombre del Color es obligatorio.", nameof(nombre));
        }

        Id = Guid.NewGuid();
        Nombre = nombre;
    }
}
