using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Paraki.Business.Interfaces;

namespace Paraki.Business.Services;

public class ResendEmailService : IEmailService
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<ResendEmailService> _logger;
    private readonly string? _apiKey;
    private readonly string _fromEmail;

    public ResendEmailService(
        IHttpClientFactory httpFactory,
        IConfiguration configuration,
        ILogger<ResendEmailService> logger)
    {
        _httpFactory = httpFactory;
        _logger = logger;
        _apiKey = configuration["Resend:ApiKey"];
        _fromEmail = configuration["Resend:FromEmail"] ?? "noreply@paraki.app";
    }

    public async Task SendAsync(string to, string subject, string htmlBody)
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            _logger.LogWarning("Resend:ApiKey não configurado. E-mail para {To} com assunto '{Subject}' não foi enviado.", to, subject);
            return;
        }

        var http = _httpFactory.CreateClient();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        var payload = new
        {
            from = _fromEmail,
            to = new[] { to },
            subject,
            html = htmlBody
        };

        var response = await http.PostAsJsonAsync("https://api.resend.com/emails", payload);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            _logger.LogError("Falha ao enviar e-mail via Resend. Status: {Status}. Body: {Body}", response.StatusCode, body);
            response.EnsureSuccessStatusCode();
        }
    }
}
