using EventFlow.Application.DTOs.Request;
using EventFlow.Domain.Entity;

namespace EventFlow.Domain.Interface
{
    public interface IParticipanteService
    {
        Task<Participante> CriarParticipanteAsync(CriarParticipanteRequest request);
        Task<IEnumerable<Participante>> ListarParticipantesAsync();
        Task<Participante?> ObterPorIdAsync(int id);
    }
}
