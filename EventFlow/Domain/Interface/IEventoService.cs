using EventFlow.Application.DTOs.Request;
using EventFlow.Domain.Entity;

namespace EventFlow.Domain.Interface
{
    public interface IEventoService
    {
        Task<Evento> CriarEventoAsync(CriarEventoRequest request, int organizadorId);
        Task<IEnumerable<Evento>> ListarEventosAsync();
        Task<Evento?> ObterPorIdAsync(int id);
        Task<bool> InativarEventoAsync(int id, int organizadorId);

        Task<Evento> AtualizarEventoAsync(int id, AtualizarEventoRequest request, int organizadorId);

        Task DesativarEventosPassados();
    }
}
