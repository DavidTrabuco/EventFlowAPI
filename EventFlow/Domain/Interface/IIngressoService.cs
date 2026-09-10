using EventFlow.Application.DTOs.Request;
using EventFlow.Domain.Entity;

namespace EventFlow.Domain.Interface
{
    public interface IIngressoService
    {
        Task<Ingresso> ComprarIngressoAsync(ComprarIngressoRequest request, int participanteId);
        Task<bool> CancelarIngressoAsync(int ingressoId, int participanteId);
        Task<bool> ValidarCheckInAsync(string codigoValidacao);
        Task<IEnumerable<Ingresso>> ListarPorParticipanteAsync(int participanteId);
    }
}
