using EventFlow.Domain.Entity;

namespace EventFlow.Domain.Interface.IRepository
{
    // Leitura via Dapper. Escrita continua no EF, dentro dos services.
    public interface IUsuarioRepository
    {
        Task<Usuario?> ObterPorEmailAsync(string email);

        Task<Usuario?> ObterUsuarioIdAsync(int id);
        Task<bool> EmailJaExisteAsync(string email);
        Task<Usuario?> ObterPorGoogleIdAsync(string googleId);
    }
}
