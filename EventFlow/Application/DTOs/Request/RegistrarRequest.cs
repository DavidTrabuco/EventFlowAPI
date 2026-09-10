using EventFlow.Domain.Enums;

namespace EventFlow.Application.DTOs.Request
{
    public class RegistrarRequest
    {
        public string Nome { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Senha { get; set; } = string.Empty;
        public PerfilUsuario Perfil { get; set; } = PerfilUsuario.Participante;
    }
}
