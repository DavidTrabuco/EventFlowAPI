using EventFlow.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace EventFlow.Application.DTOs.Request
{
    public class RegistrarRequest
    {
        public string Nome { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        [MinLength(6, ErrorMessage = "A senha deve ter no mínimo 6 caracteres.")]
        public string Senha { get; set; } = string.Empty;
        public PerfilUsuario Perfil { get; set; } = PerfilUsuario.Participante;
    }
}
