using System.ComponentModel.DataAnnotations.Schema;

namespace EventFlow.Domain.Entity
{
    public class Sessao
    {
        public int Id { get; set; }

        public int UsuarioId { get; set; }
        public Usuario? Usuario { get; set; }

        // Guardamos o HASH do valor, nunca o valor original: se a tabela
        // vazar, o atacante nao consegue reconstruir sessao nenhuma.
        public string TokenHash { get; set; } = string.Empty;

        public DateTime CriadoEm { get; set; }
        public DateTime ExpiraEm { get; set; }
        public DateTime? EncerradaEm { get; set; }

        [NotMapped]
        public bool Ativa => EncerradaEm is null && DateTime.UtcNow < ExpiraEm;
    }
}
