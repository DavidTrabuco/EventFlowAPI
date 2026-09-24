using System.Data;
using Dapper;
using EventFlow.Domain.Entity;
using EventFlow.Domain.Interface.IRepository;
using EventFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EventFlow.Infrastructure.Repositories
{
    public class EventoRepository : IEventoRepository
    {
        private readonly EventFlowDbContext _db;

        public EventoRepository(EventFlowDbContext db) => _db = db;

        private IDbConnection Conexao => _db.Database.GetDbConnection();

        public Task<IEnumerable<Evento>> ListarAsync() =>
            Conexao.QueryAsync<Evento>("SELECT * FROM eventos ORDER BY data_hora");

        public Task<Evento?> ObterPorIdAsync(int id) =>
            Conexao.QueryFirstOrDefaultAsync<Evento>(
                "SELECT * FROM eventos WHERE id = @id", new { id });
    }
}
