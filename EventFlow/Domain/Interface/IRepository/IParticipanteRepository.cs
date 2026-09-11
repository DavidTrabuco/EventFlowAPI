using EventFlow.Domain.Entity;

namespace EventFlow.Domain.Interface.IRepository
{
    public interface IParticipanteRepository
    {
        Task<IEnumerable<Participante>> ListarAsync();
        Task<Participante?> ObterPorIdAsync(int id);
        Task<Participante?> ObterPorUsuarioIdAsync(int usuarioId);
        Task<bool> CpfJaExisteAsync(string cpf);
        Task<bool> EmailJaExisteAsync(string email);
        Task<bool> UsuarioJaTemPerfilAsync(int usuarioId);
    }
}
