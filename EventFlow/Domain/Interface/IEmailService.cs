namespace EventFlow.Domain.Interface
{
    public interface IEmailService
    {
        Task EnviarAsync(string destinatario, string assunto, string corpoHtml);
    }
}
