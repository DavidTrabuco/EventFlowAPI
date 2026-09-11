using EventFlow.Domain.Entity;

namespace EventFlow.Domain.Interface.IRepository
{
    public interface IEventoRepository
    {
        Task<IEnumerable<Evento>> ListarAsync();
        Task<Evento?> ObterPorIdAsync(int id);
    }
}
