namespace IGE.Informes.Application.Common.Dtos;

public sealed record PagedResult<T>(IReadOnlyCollection<T> Items, int Pagina, int TamanioPagina, int TotalItems)
{
    public int TotalPaginas => (int)Math.Ceiling(TotalItems / (double)TamanioPagina);
}
