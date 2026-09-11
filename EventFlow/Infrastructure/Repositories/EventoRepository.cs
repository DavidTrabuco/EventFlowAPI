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
        // O EF grava decimal como TEXT no SQLite. Sem o CAST, o Dapper converte
        // esse texto pela cultura da maquina — e em pt-BR o ponto e separador
        // de milhar: '120.0' viraria 1200. Por isso nao da para usar SELECT *.
        private const string Campos = """
            Id, Titulo, Descricao, DataHora, Local, CapacidadeMaxima,
            IngressosVendidos, CAST(PrecoIngresso AS REAL) AS PrecoIngresso, Ativo
            """;                        

        private readonly EventFlowDbContext _db;

        public EventoRepository(EventFlowDbContext db) => _db = db;

        private IDbConnection Conexao => _db.Database.GetDbConnection();

        public Task<IEnumerable<Evento>> ListarAsync() =>
            Conexao.QueryAsync<Evento>($"SELECT {Campos} FROM Eventos ORDER BY DataHora");

        public Task<Evento?> ObterPorIdAsync(int id) =>
            Conexao.QueryFirstOrDefaultAsync<Evento>(
                $"SELECT {Campos} FROM Eventos WHERE Id = @id", new { id });
    }
}
