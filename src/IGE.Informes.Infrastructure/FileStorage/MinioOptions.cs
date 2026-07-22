namespace IGE.Informes.Infrastructure.FileStorage;

public sealed class MinioOptions
{
    public const string SectionName = "Minio";

    public string Endpoint { get; init; } = string.Empty;
    public string AccessKey { get; init; } = string.Empty;
    public string SecretKey { get; init; } = string.Empty;
    public string BucketName { get; init; } = "ige-informes";
    public bool UseSsl { get; init; }
    public int UrlDescargaExpiracionSegundos { get; init; } = 300;
}
