using EventFlow.Domain.Entity;

namespace EventFlow.Application.DTOs.Response
{
    public class ParticipanteResponse
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Cpf { get; set; } = string.Empty;

        public static ParticipanteResponse De(Participante participante) => new()
        {
            Id = participante.Id,
            Nome = participante.Nome,
            Email = participante.Email,
            Cpf = participante.Cpf
        };
    }
}
