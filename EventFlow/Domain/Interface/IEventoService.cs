using EventFlow.Application.DTOs.Request;
using EventFlow.Domain.Entity;

namespace EventFlow.Domain.Interface
{
    public interface IEventoService
    {
        Task<Evento> CriarEventoAsync(CriarEventoRequest request);
        Task<IEnumerable<Evento>> ListarEventosAsync();
        Task<Evento?> ObterPorIdAsync(int id);
        Task<bool> InativarEventoAsync(int id);

        Task<Evento> AtualizarEventoAsync(int id, AtualizarEventoRequest request);
    }
}
