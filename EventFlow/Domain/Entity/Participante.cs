namespace EventFlow.Domain.Entity
{
    public class Participante
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Cpf { get; set; } = string.Empty;

        public int UsuarioId { get; set; }
        public Usuario? Usuario { get; set; }

        public Participante() { }

        public Participante(int id, string nome, string email, string cpf, int usuarioId)
        {
            Id = id;
            Nome = nome;
            Email = email;
            Cpf = cpf;
            UsuarioId = usuarioId;
        }
    }
}
