using System.ComponentModel.DataAnnotations;

namespace EventFlow.Domain.Options;


public class GoogleAuthOptions
{
    public const string SectionName = "Authentication:Google";

    [Required(ErrorMessage = "Authentication:Google:ClientId nao configurado.")]
    public string ClientId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Authentication:Google:ClientSecret nao configurado. Rode: dotnet user-secrets set \"Authentication:Google:ClientSecret\" \"<valor>\"")]
    public string ClientSecret { get; set; } = string.Empty;
}
