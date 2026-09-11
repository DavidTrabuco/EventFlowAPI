using System.Data;
using Dapper;
using EventFlow.Domain.Entity;
using EventFlow.Domain.Enums;
using EventFlow.Domain.Interface.IRepository;
using EventFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EventFlow.Infrastructure.Repositories
{
    public class IngressoRepository : IIngressoRepository
    {
        // ValorPago tambem e decimal gravado como TEXT: precisa do CAST.
        private const string Campos = """
            Id, EventoId, ParticipanteId, DataHoraCompra,
            CAST(ValorPago AS REAL) AS ValorPago, CodigoValidacao, Status
            """;

        private readonly EventFlowDbContext _db;

        public IngressoRepository(EventFlowDbContext db) => _db = db;

        private IDbConnection Conexao => _db.Database.GetDbConnection();

        public Task<IEnumerable<Ingresso>> ListarPorParticipanteAsync(int participanteId) =>
            Conexao.QueryAsync<Ingresso>(
                $"SELECT {Campos} FROM Ingressos WHERE ParticipanteId = @participanteId",
                new { participanteId });

        // RN03: so ingressos ATIVOS contam. O valor vem do enum, nao de um
        // literal — se StatusIngresso mudar, a consulta acompanha.
        public Task<int> ContarAtivosAsync(int eventoId, int participanteId) =>
            Conexao.ExecuteScalarAsync<int>(
                """
                SELECT COUNT(1) FROM Ingressos
                WHERE EventoId = @eventoId AND ParticipanteId = @participanteId
                  AND Status = @status
                """,
                new { eventoId, participanteId, status = (int)StatusIngresso.Ativo });

        public Task<bool> CodigoJaExisteAsync(string codigo) =>
            Conexao.ExecuteScalarAsync<bool>(
                "SELECT EXISTS (SELECT 1 FROM Ingressos WHERE CodigoValidacao = @codigo)",
                new { codigo });
    }
}
