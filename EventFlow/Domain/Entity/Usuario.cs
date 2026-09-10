using EventFlow.Domain.Enums;

namespace EventFlow.Domain.Entity
{
    public class Usuario
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string SenhaHash { get; set; } = string.Empty;
        public PerfilUsuario Perfil { get; set; } = PerfilUsuario.Participante;
    }
}
