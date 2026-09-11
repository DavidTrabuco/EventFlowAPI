using EventFlow.Domain.Entity;

namespace EventFlow.Domain.Interface.IRepository
{
    public interface IIngressoRepository
    {
        Task<IEnumerable<Ingresso>> ListarPorParticipanteAsync(int participanteId);
        Task<int> ContarAtivosAsync(int eventoId, int participanteId);
        Task<bool> CodigoJaExisteAsync(string codigoValidacao);
    }
}
