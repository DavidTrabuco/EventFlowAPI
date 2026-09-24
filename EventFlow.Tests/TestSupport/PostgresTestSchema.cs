using Npgsql;

namespace EventFlow.Tests.TestSupport;

/// <summary>
/// Um schema isolado dentro do banco "eventflow" do docker-compose.yml (precisa
/// estar rodando: "docker compose up postgres"). Cada teste ganha seu proprio
/// conjunto de tabelas sem precisar de um banco Postgres inteiro por teste -
/// substitui o antigo SqliteConnection("Filename=:memory:").
///
/// A conexao fica aberta e com o search_path preso no schema do teste (em vez
/// de guardar so a connection string) porque o Npgsql reaproveita conexoes do
/// pool por trás dos panos: se cada UseNpgsql(connectionString) abrisse a sua
/// propria conexao, uma vinda do pool poderia nao ter o SET search_path ainda
/// aplicado. Passando a mesma conexao ja configurada pro EF Core, o schema
/// certo fica garantido.
/// </summary>
internal sealed class PostgresTestSchema
{
    private const string ConexaoBase =
        "Host=localhost;Port=5432;Database=eventflow;Username=eventflow;Password=eventflow";

    public string Nome { get; }
    public NpgsqlConnection Conexao { get; }

    private PostgresTestSchema(string nome, NpgsqlConnection conexao)
    {
        Nome = nome;
        Conexao = conexao;
    }

    public static PostgresTestSchema Criar()
    {
        var nome = $"test_{Guid.NewGuid():N}";

        var conexao = new NpgsqlConnection(ConexaoBase);
        conexao.Open();

        using (var comando = conexao.CreateCommand())
        {
            comando.CommandText = $"CREATE SCHEMA \"{nome}\"";
            comando.ExecuteNonQuery();
        }

        using (var comando = conexao.CreateCommand())
        {
            comando.CommandText = $"SET search_path TO \"{nome}\"";
            comando.ExecuteNonQuery();
        }

        return new PostgresTestSchema(nome, conexao);
    }

    public void Derrubar()
    {
        using (var comando = Conexao.CreateCommand())
        {
            comando.CommandText = $"DROP SCHEMA IF EXISTS \"{Nome}\" CASCADE";
            comando.ExecuteNonQuery();
        }

        Conexao.Dispose();
    }
}
