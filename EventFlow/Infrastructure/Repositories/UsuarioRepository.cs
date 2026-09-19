using System.Data;
using Dapper;
using EventFlow.Domain.Entity;
using EventFlow.Domain.Interface.IRepository;
using EventFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EventFlow.Infrastructure.Repositories
{
    public class UsuarioRepository : IUsuarioRepository
    {
        private readonly EventFlowDbContext _db;

        public UsuarioRepository(EventFlowDbContext db) => _db = db;

        // A MESMA conexao que o EF usa, em vez de abrir uma segunda.
        // No SQLite isso importa: varios leitores, mas um escritor so.
        private IDbConnection Conexao => _db.Database.GetDbConnection();

        public Task<Usuario?> ObterPorEmailAsync(string email) =>
            Conexao.QueryFirstOrDefaultAsync<Usuario>(
                "SELECT * FROM Usuarios WHERE Email = @email", new { email });

        public Task<bool> EmailJaExisteAsync(string email) =>
            Conexao.ExecuteScalarAsync<bool>(
                "SELECT EXISTS (SELECT 1 FROM Usuarios WHERE Email = @email)", new { email });

        public Task<Usuario?> ObterPorGoogleIdAsync(string googleId) =>
            Conexao.QueryFirstOrDefaultAsync<Usuario>(
                "SELECT * FROM Usuarios WHERE GoogleId = @googleId", new { googleId });


        public Task<Usuario?> ObterUsuarioIdAsync(int id) =>
        Conexao.QueryFirstOrDefaultAsync<Usuario>(
            "SELECT * FROM Usuarios WHERE Id = @id", new { id });
    }
}
