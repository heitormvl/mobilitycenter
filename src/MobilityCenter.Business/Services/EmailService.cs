using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;
using MobilityCenter.Business.Interfaces;

namespace MobilityCenter.Business.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config) => _config = config;

    public async Task EnviarConfirmacaoAsync(string email, string displayName, string confirmUrl)
    {
        var fromAddress = _config["Email:FromAddress"]
            ?? throw new InvalidOperationException("Email:FromAddress não configurado.");
        var fromName = _config["Email:FromName"] ?? "MobilityCenter";
        var host = _config["Email:SmtpHost"]
            ?? throw new InvalidOperationException("Email:SmtpHost não configurado.");
        var port = int.Parse(_config["Email:SmtpPort"] ?? "587");
        var user = _config["Email:SmtpUser"]
            ?? throw new InvalidOperationException("Email:SmtpUser não configurado.");
        var password = _config["Email:SmtpPassword"]
            ?? throw new InvalidOperationException("Email:SmtpPassword não configurado.");

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromAddress));
        message.To.Add(new MailboxAddress(displayName, email));
        message.Subject = "Confirme seu e-mail — MobilityCenter";

        var body = new BodyBuilder
        {
            HtmlBody = $"""
                <!DOCTYPE html>
                <html lang="pt-BR">
                <body style="margin:0;padding:0;font-family:Arial,sans-serif;background:#f4f4f4;">
                  <table width="100%" cellpadding="0" cellspacing="0">
                    <tr><td align="center" style="padding:40px 0;">
                      <table width="560" cellpadding="0" cellspacing="0" style="background:#ffffff;border-radius:8px;overflow:hidden;">
                        <tr><td style="background:#16a34a;padding:32px;text-align:center;">
                          <h1 style="color:#ffffff;margin:0;font-size:24px;">MobilityCenter</h1>
                        </td></tr>
                        <tr><td style="padding:40px 48px;">
                          <p style="font-size:16px;color:#374151;margin:0 0 16px;">Olá, <strong>{displayName}</strong>!</p>
                          <p style="font-size:15px;color:#6b7280;margin:0 0 32px;">
                            Clique no botão abaixo para confirmar seu e-mail e ativar sua conta.
                          </p>
                          <div style="text-align:center;margin-bottom:32px;">
                            <a href="{confirmUrl}"
                               style="display:inline-block;background:#16a34a;color:#ffffff;padding:14px 32px;
                                      border-radius:6px;font-size:15px;font-weight:bold;text-decoration:none;">
                              Confirmar e-mail
                            </a>
                          </div>
                          <p style="font-size:13px;color:#9ca3af;margin:0;">
                            Se você não criou uma conta, ignore este e-mail.<br>
                            O link expira em <strong>24 horas</strong>.
                          </p>
                        </td></tr>
                      </table>
                    </td></tr>
                  </table>
                </body>
                </html>
                """,
            TextBody = $"Olá, {displayName}!\n\nConfirme seu e-mail acessando o link abaixo:\n{confirmUrl}\n\nO link expira em 24 horas."
        };
        message.Body = body.ToMessageBody();

        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(host, port, SecureSocketOptions.StartTls);
        await smtp.AuthenticateAsync(user, password);
        await smtp.SendAsync(message);
        await smtp.DisconnectAsync(true);
    }
}
