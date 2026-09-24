using System.ComponentModel.DataAnnotations;

namespace EventFlow.Domain.Options;

public class EmailOptions
{
    public const string SectionName = "Email";

    [Required(ErrorMessage = "Email:From nao configurado. Rode: dotnet user-secrets set Email:From <seu-email@gmail.com>")]
    [EmailAddress(ErrorMessage = "Email:From nao e um endereco de e-mail valido.")]
    public string From { get; set; } = string.Empty;

    // Nao e a senha da conta Google: e uma "senha de app" gerada em
    // myaccount.google.com/apppasswords, especifica pra essa aplicacao.
    [Required(ErrorMessage = "Email:AppPassword nao configurada. Rode: dotnet user-secrets set Email:AppPassword <valor>")]
    public string AppPassword { get; set; } = string.Empty;

    public string SmtpHost { get; set; } = "smtp.gmail.com";
    public int SmtpPort { get; set; } = 587;
}
