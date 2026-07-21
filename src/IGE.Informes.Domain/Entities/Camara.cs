namespace IGE.Informes.Domain.Entities;

public enum TipoCamara
{
    Domo,
    Lpr,
}

public sealed class Camara : IAuditable
{
    public Guid Id { get; private set; }
    public string Codigo { get; private set; } = string.Empty;
    public string? Ubicacion { get; private set; }
    public TipoCamara Tipo { get; private set; }

    private Camara()
    {
    }

    public Camara(string codigo, TipoCamara tipo, string? ubicacion = null)
    {
        if (string.IsNullOrWhiteSpace(codigo))
        {
            throw new ArgumentException("El código de la Cámara es obligatorio.", nameof(codigo));
        }

        Id = Guid.NewGuid();
        Codigo = codigo;
        Tipo = tipo;
        Ubicacion = ubicacion;
    }

    public void CompletarUbicacion(string ubicacion)
    {
        if (string.IsNullOrWhiteSpace(ubicacion))
        {
            throw new ArgumentException("La ubicación no puede estar vacía.", nameof(ubicacion));
        }

        Ubicacion = ubicacion;
    }

    public void CambiarTipo(TipoCamara tipo)
    {
        Tipo = tipo;
    }
}
