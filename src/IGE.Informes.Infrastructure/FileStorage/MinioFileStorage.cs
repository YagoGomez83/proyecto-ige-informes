using IGE.Informes.Application.Common.Interfaces;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;

namespace IGE.Informes.Infrastructure.FileStorage;

/// <summary>
/// Implementación contra MinIO (S3-compatible). El bucket se crea privado
/// (sin política pública) — toda lectura pasa por URL prefirmada de corta
/// expiración, nunca se expone una URL directa permanente (ver
/// docs/06-seguridad-amenazas.md, "Information Disclosure — archivos").
/// </summary>
public sealed class MinioFileStorage : IFileStorage
{
    private readonly IMinioClient _client;
    private readonly IMinioClient _clientParaUrlsPublicas;
    private readonly MinioOptions _options;

    public MinioFileStorage(IOptions<MinioOptions> options)
    {
        _options = options.Value;
        _client = new MinioClient()
            .WithEndpoint(_options.Endpoint)
            .WithCredentials(_options.AccessKey, _options.SecretKey)
            .WithSSL(_options.UseSsl)
            .Build();

        // Cliente separado solo para firmar URLs prefirmadas con el host
        // público (ver comentario en MinioOptions.EndpointPublico) — el SDK
        // incluye el endpoint del cliente en la firma de la URL, así que no
        // alcanza con reemplazar el host del string resultante a mano.
        _clientParaUrlsPublicas = new MinioClient()
            .WithEndpoint(_options.EndpointPublico ?? _options.Endpoint)
            .WithCredentials(_options.AccessKey, _options.SecretKey)
            .WithSSL(_options.UseSslPublico ?? _options.UseSsl)
            .Build();
    }

    public async Task<string> SubirAsync(string nombreArchivo, Stream contenido, string tipoMime, CancellationToken cancellationToken = default)
    {
        await AsegurarBucketAsync(cancellationToken);

        var clave = $"{Guid.NewGuid():N}/{nombreArchivo}";

        var putArgs = new PutObjectArgs()
            .WithBucket(_options.BucketName)
            .WithObject(clave)
            .WithStreamData(contenido)
            .WithObjectSize(contenido.Length)
            .WithContentType(tipoMime);

        await _client.PutObjectAsync(putArgs, cancellationToken);

        return clave;
    }

    public async Task<string> ObtenerUrlDescargaAsync(string clave, CancellationToken cancellationToken = default)
    {
        var args = new PresignedGetObjectArgs()
            .WithBucket(_options.BucketName)
            .WithObject(clave)
            .WithExpiry(_options.UrlDescargaExpiracionSegundos);

        return await _clientParaUrlsPublicas.PresignedGetObjectAsync(args);
    }

    public async Task<byte[]> DescargarAsync(string clave, CancellationToken cancellationToken = default)
    {
        using var destino = new MemoryStream();

        var args = new GetObjectArgs()
            .WithBucket(_options.BucketName)
            .WithObject(clave)
            .WithCallbackStream((stream, ct) => stream.CopyToAsync(destino, ct));

        await _client.GetObjectAsync(args, cancellationToken);

        return destino.ToArray();
    }

    public async Task EliminarAsync(string clave, CancellationToken cancellationToken = default)
    {
        var args = new RemoveObjectArgs()
            .WithBucket(_options.BucketName)
            .WithObject(clave);

        await _client.RemoveObjectAsync(args, cancellationToken);
    }

    private async Task AsegurarBucketAsync(CancellationToken cancellationToken)
    {
        var existeArgs = new BucketExistsArgs().WithBucket(_options.BucketName);
        var existe = await _client.BucketExistsAsync(existeArgs, cancellationToken);

        if (!existe)
        {
            var crearArgs = new MakeBucketArgs().WithBucket(_options.BucketName);
            await _client.MakeBucketAsync(crearArgs, cancellationToken);
        }
    }
}
