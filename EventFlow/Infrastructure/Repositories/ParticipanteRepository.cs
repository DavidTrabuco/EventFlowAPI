using System.Data;
using Dapper;
using EventFlow.Domain.Entity;
using EventFlow.Domain.Interface.IRepository;
using EventFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EventFlow.Infrastructure.Repositories
{
    public class ParticipanteRepository : IParticipanteRepository
    {
        private readonly EventFlowDbContext _db;

        public ParticipanteRepository(EventFlowDbContext db) => _db = db;

        private IDbConnection Conexao => _db.Database.GetDbConnection();

        public Task<IEnumerable<Participante>> ListarAsync() =>
            Conexao.QueryAsync<Participante>("SELECT * FROM participantes ORDER BY nome");

        public Task<Participante?> ObterPorIdAsync(int id) =>
            Conexao.QueryFirstOrDefaultAsync<Participante>(
                "SELECT * FROM participantes WHERE id = @id", new { id });

        public Task<Participante?> ObterPorUsuarioIdAsync(int usuarioId) =>
            Conexao.QueryFirstOrDefaultAsync<Participante>(
                "SELECT * FROM participantes WHERE usuario_id = @usuarioId", new { usuarioId });

        public Task<bool> CpfJaExisteAsync(string cpf) =>
            Conexao.ExecuteScalarAsync<bool>(
                "SELECT EXISTS (SELECT 1 FROM participantes WHERE cpf = @cpf)", new { cpf });

        public Task<bool> EmailJaExisteAsync(string email) =>
            Conexao.ExecuteScalarAsync<bool>(
                "SELECT EXISTS (SELECT 1 FROM participantes WHERE email = @email)", new { email });

        public Task<bool> UsuarioJaTemPerfilAsync(int usuarioId) =>
            Conexao.ExecuteScalarAsync<bool>(
                "SELECT EXISTS (SELECT 1 FROM participantes WHERE usuario_id = @usuarioId)",
                new { usuarioId });
    }
}
