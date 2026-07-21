using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IGE.Informes.Application.Informes.Commands.GenerarInformeDesdeCaso;

public sealed class GenerarInformeDesdeCasoCommandHandler(IAppDbContext dbContext, ICurrentUserService currentUserService)
    : IRequestHandler<GenerarInformeDesdeCasoCommand, Guid>
{
    private const int MaxIntentosGenerarIdRegistro = 3;

    public async Task<Guid> Handle(GenerarInformeDesdeCasoCommand request, CancellationToken cancellationToken)
    {
        var casoExiste = await dbContext.CasosAnalisis
            .AnyAsync(c => c.Id == request.CasoAnalisisId, cancellationToken);
        if (!casoExiste)
        {
            throw new EntidadNoEncontradaException(nameof(CasoAnalisis), request.CasoAnalisisId);
        }

        var dependenciaExiste = await dbContext.Dependencias
            .AnyAsync(d => d.Id == request.DependenciaDestinoId, cancellationToken);
        if (!dependenciaExiste)
        {
            throw new EntidadNoEncontradaException(nameof(Dependencia), request.DependenciaDestinoId);
        }

        var usuarioId = currentUserService.UsuarioId
            ?? throw new ForbiddenAccessException("No hay un usuario autenticado.");

        for (var intento = 1; intento <= MaxIntentosGenerarIdRegistro; intento++)
        {
            var causa = new Causa(request.CausaCaratula, request.CausaNroPiezaSumarial, request.CausaCircunscripcionJudicial);
            dbContext.Causas.Add(causa);

            var idRegistro = await GenerarIdRegistroAsync(cancellationToken);

            var informe = new Informe(
                idRegistro,
                DateOnly.FromDateTime(DateTime.UtcNow),
                request.CasoAnalisisId,
                request.DependenciaDestinoId,
                usuarioId,
                causa.Id);

            dbContext.Informes.Add(informe);

            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
                return informe.Id;
            }
            catch (DbUpdateException) when (intento < MaxIntentosGenerarIdRegistro)
            {
                // Carrera con otra generación simultánea de Informe: el índice
                // único de IdRegistro rechazó el correlativo — reintentar con
                // el siguiente número en vez de propagar un duplicado.
            }
        }

        throw new InvalidOperationException("No se pudo generar un ID Registro único para el Informe tras varios intentos.");
    }

    private async Task<string> GenerarIdRegistroAsync(CancellationToken cancellationToken)
    {
        var anio = DateTime.UtcNow.Year;
        var prefijoAnio = $"/{anio}";

        var ultimoCorrelativo = await dbContext.Informes
            .Where(i => i.IdRegistro.EndsWith(prefijoAnio))
            .Select(i => i.IdRegistro.Substring(0, i.IdRegistro.IndexOf("/")))
            .ToListAsync(cancellationToken);

        var siguiente = ultimoCorrelativo
            .Select(numero => int.TryParse(numero, out var valor) ? valor : 0)
            .DefaultIfEmpty(0)
            .Max() + 1;

        return $"{siguiente}/{anio}";
    }
}
