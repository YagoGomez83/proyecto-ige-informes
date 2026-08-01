namespace IGE.Informes.Domain.Entities;

public sealed class VehiculoImagen : IAuditable
{
    public Guid Id { get; private set; }
    public Guid VehiculoId { get; private set; }
    public string ImagenPath { get; private set; } = string.Empty;
    public DateTime FechaCarga { get; private set; }
    public Guid SubidaPorUsuarioId { get; private set; }

    private VehiculoImagen()
    {
    }

    public VehiculoImagen(Guid vehiculoId, string imagenPath, Guid subidaPorUsuarioId)
    {
        if (vehiculoId == Guid.Empty)
        {
            throw new ArgumentException("El Vehículo de la imagen es obligatorio.", nameof(vehiculoId));
        }

        if (string.IsNullOrWhiteSpace(imagenPath))
        {
            throw new ArgumentException("La ruta de la imagen es obligatoria.", nameof(imagenPath));
        }

        if (subidaPorUsuarioId == Guid.Empty)
        {
            throw new ArgumentException("El usuario que sube la imagen es obligatorio.", nameof(subidaPorUsuarioId));
        }

        Id = Guid.NewGuid();
        VehiculoId = vehiculoId;
        ImagenPath = imagenPath;
        SubidaPorUsuarioId = subidaPorUsuarioId;
        FechaCarga = DateTime.UtcNow;
    }
}
