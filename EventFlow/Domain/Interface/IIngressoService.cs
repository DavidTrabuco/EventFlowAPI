using EventFlow.Application.DTOs.Request;
using EventFlow.Domain.Entity;

namespace EventFlow.Domain.Interface
{
    public interface IIngressoService
    {
        Task<Ingresso> ComprarIngressoAsync(ComprarIngressoRequest request);
        Task<bool> CancelarIngressoAsync(int ingressoId);
        Task<bool> ValidarCheckInAsync(string codigoValidacao);
        Task<IEnumerable<Ingresso>> ListarPorParticipanteAsync(int participanteId);

        
    }
}
