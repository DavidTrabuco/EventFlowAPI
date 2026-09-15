using System.ComponentModel.DataAnnotations;

namespace EventFlow.Domain.Options
{
    public class JwtOptions


    {
        public const string SectionName = "Jwt";

        [Required(ErrorMessage = "Jwt:Key nao configurada. Rode: dotnet user-secrets set Jwt:Key <valor>")]
        [MinLength(32, ErrorMessage = "Jwt:Key deve ter no minimo 32 caracteres.")]
        public string Key { get; set; } = string.Empty;

        [Required(ErrorMessage = "Jwt:Issuer nao configurada. Rode: dotnet user-secrets set Jwt:Issuer <valor>")]
        public string Issuer { get; set; } = string.Empty;
        [Required(ErrorMessage = "Jwt:Audience nao configurada. Rode: dotnet user-secrets set Jwt:Audience <valor>")]
        public string Audience { get; set; } = string.Empty;
    }
}
