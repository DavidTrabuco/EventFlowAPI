using EventFlow.Domain.Enums;

namespace EventFlow.Domain.Entity
{
    public class Usuario
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        // Nula quando o usuario entrou so via Google (nunca cadastrou senha local).
        public string? SenhaHash { get; set; }

        // "sub" do token do Google. Nulo para contas locais. Usado para
        // reconhecer o usuario nos proximos logins, ja que e imutavel
        // (diferente do email, que a pessoa pode trocar).
        public string? GoogleId { get; set; }

        public PerfilUsuario Perfil { get; set; } = PerfilUsuario.Participante;
    }
}
