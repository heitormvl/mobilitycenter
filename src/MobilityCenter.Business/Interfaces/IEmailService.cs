namespace MobilityCenter.Business.Interfaces;

public interface IEmailService
{
    Task EnviarConfirmacaoAsync(string email, string displayName, string confirmUrl);
}
