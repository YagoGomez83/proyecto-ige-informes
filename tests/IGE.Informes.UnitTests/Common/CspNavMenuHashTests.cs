using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace IGE.Informes.UnitTests.Common;

/// <summary>
/// CspMiddleware.cs (IGE.Informes.Web) autoriza el atributo onclick inline
/// de NavMenu.razor en la CSP vía un hash SHA-256 hardcodeado como string
/// literal ('unsafe-hashes'), siguiendo el patrón documentado por
/// Microsoft para Blazor Web Apps. Si alguien edita ese onclick sin
/// recordar recalcular el hash, el sidebar de mobile se rompe en
/// producción sin ningún error de build — este test recalcula el hash a
/// partir del contenido real de NavMenu.razor y lo compara contra el
/// literal leído del código fuente real de CspMiddleware.cs (no una
/// copia hardcodeada acá: una tercera copia podría divergir de ambos
/// archivos de producción sin que el test lo note, o "arreglarse" editando
/// el test en vez del código).
/// </summary>
public class CspNavMenuHashTests
{
    [Fact]
    public void Hash_del_onclick_inline_de_NavMenu_coincide_con_el_hardcodeado_en_CspMiddleware()
    {
        var navMenuPath = ResolverRutaRepo("src", "IGE.Informes.Web", "Components", "Layout", "NavMenu.razor");
        var contenido = File.ReadAllText(navMenuPath);

        var match = Regex.Match(contenido, @"onclick=""([^""]*)""");
        Assert.True(match.Success, $"No se encontró ningún atributo onclick inline en {navMenuPath} — si se quitó, el hash 'unsafe-hashes' en CspMiddleware.cs ya no hace falta y debería eliminarse.");

        var valorOnclick = match.Groups[1].Value;
        var hashCalculado = $"sha256-{Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(valorOnclick)))}";

        var hashEsperadoEnCspMiddleware = LeerHashHardcodeadoDeCspMiddleware();

        Assert.Equal(hashEsperadoEnCspMiddleware, hashCalculado);
    }

    private static string LeerHashHardcodeadoDeCspMiddleware()
    {
        var cspMiddlewarePath = ResolverRutaRepo("src", "IGE.Informes.Web", "CspMiddleware.cs");
        var contenido = File.ReadAllText(cspMiddlewarePath);

        var match = Regex.Match(contenido, @"'(sha256-[A-Za-z0-9+/]+=*)'");
        Assert.True(match.Success, $"No se encontró ningún hash 'sha256-...' hardcodeado en {cspMiddlewarePath} — si cambió la forma de autorizar el onclick de NavMenu (ej. a nonce), este test quedó obsoleto y hay que actualizarlo.");

        return match.Groups[1].Value;
    }

    private static string ResolverRutaRepo(params string[] segmentos)
    {
        var directorio = new DirectoryInfo(AppContext.BaseDirectory);
        while (directorio is not null && !File.Exists(Path.Combine(directorio.FullName, "IGE.Informes.sln")))
        {
            directorio = directorio.Parent;
        }

        Assert.True(directorio is not null, $"No se encontró IGE.Informes.sln subiendo desde {AppContext.BaseDirectory} — no se pudo resolver la raíz del repo.");

        return Path.Combine([directorio!.FullName, .. segmentos]);
    }
}
