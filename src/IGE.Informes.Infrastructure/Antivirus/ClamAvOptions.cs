namespace IGE.Informes.Infrastructure.Antivirus;

public sealed class ClamAvOptions
{
    public const string SectionName = "ClamAv";

    public string Host { get; init; } = "clamav";
    public int Port { get; init; } = 3310;
    public int TimeoutSegundos { get; init; } = 30;
}
