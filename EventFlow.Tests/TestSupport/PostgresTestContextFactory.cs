using EventFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EventFlow.Tests.TestSupport;

/// <summary>
/// Cria um EventFlowDbContext isolado em um schema Postgres proprio (ver
/// PostgresTestSchema) e derruba esse schema quando o contexto e descartado.
/// </summary>
internal static class PostgresTestContextFactory
{
    public static EventFlowDbContext CriarContexto()
    {
        var schema = PostgresTestSchema.Criar();

        // UseSnakeCaseNamingConvention precisa bater com o Program.cs: as
        // migrations ja criam as tabelas em snake_case, e sem isso aqui o EF
        // gera SQL procurando os nomes PascalCase originais (que nao existem).
        var options = new DbContextOptionsBuilder<EventFlowDbContext>()
            .UseNpgsql(schema.Conexao)
            .UseSnakeCaseNamingConvention()
            .Options;

        // As migrations geradas carregam [DbContext(typeof(EventFlowDbContext))],
        // e o EF Core so as encontra quando o tipo CONCRETO do contexto bate com
        // esse atributo - por isso a migracao roda aqui, com o tipo puro, antes
        // de devolver o SchemaScopedDbContext (que precisa ser subclasse pra
        // conseguir derrubar o schema no Dispose).
        using (var migrador = new EventFlowDbContext(options))
        {
            migrador.Database.Migrate();
        }

        return new SchemaScopedDbContext(options, schema);
    }

    private sealed class SchemaScopedDbContext : EventFlowDbContext
    {
        private readonly PostgresTestSchema _schema;

        public SchemaScopedDbContext(DbContextOptions<EventFlowDbContext> options, PostgresTestSchema schema)
            : base(options)
        {
            _schema = schema;
        }

        public override void Dispose()
        {
            base.Dispose();
            _schema.Derrubar();
        }

        public override async ValueTask DisposeAsync()
        {
            await base.DisposeAsync();
            _schema.Derrubar();
        }
    }
}
