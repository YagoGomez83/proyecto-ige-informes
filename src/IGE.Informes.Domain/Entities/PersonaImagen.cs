namespace IGE.Informes.Domain.Entities;

public sealed class PersonaImagen : IAuditable
{
    public Guid Id { get; private set; }
    public Guid PersonaId { get; private set; }
    public string ImagenPath { get; private set; } = string.Empty;
    public DateTime FechaCarga { get; private set; }
    public Guid SubidaPorUsuarioId { get; private set; }

    private PersonaImagen()
    {
    }

    public PersonaImagen(Guid personaId, string imagenPath, Guid subidaPorUsuarioId)
    {
        if (personaId == Guid.Empty)
        {
            throw new ArgumentException("La Persona de la imagen es obligatoria.", nameof(personaId));
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
        PersonaId = personaId;
        ImagenPath = imagenPath;
        SubidaPorUsuarioId = subidaPorUsuarioId;
        FechaCarga = DateTime.UtcNow;
    }
}
