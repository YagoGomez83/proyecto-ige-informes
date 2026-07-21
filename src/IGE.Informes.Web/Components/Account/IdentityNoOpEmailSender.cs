using IGE.Informes.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace IGE.Informes.Web.Components.Account;

/// <summary>
/// No-op: el sistema no envía mails en Fase 0. Se reemplaza por un sender
/// real (SMTP institucional) cuando el flujo de confirmación de cuenta lo
/// requiera — hasta entonces, los usuarios se crean directamente en estado
/// confirmado por el Administrador.
/// </summary>
public sealed class IdentityNoOpEmailSender : IEmailSender<ApplicationUser>
{
    public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink) => Task.CompletedTask;
    public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink) => Task.CompletedTask;
    public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode) => Task.CompletedTask;
}
