namespace IGE.Informes.Application.Common.Security;

public static class Roles
{
    public const string Analista = "Analista";
    public const string Supervisor = "Supervisor";
    public const string Admin = "Admin";

    public static readonly string[] Todos = [Analista, Supervisor, Admin];
}
