using System.ComponentModel.DataAnnotations.Schema;

namespace EventFlow.Domain.Entity
{
    public class RefreshToken
    {
        public int Id { get; set; }

        public int UsuarioId { get; set; }
        public Usuario? Usuario { get; set; }

        // Guardamos o HASH, nunca o valor original: se a tabela vazar,
        // o atacante nao consegue reconstruir as sessoes.
        public string TokenHash { get; set; } = string.Empty;

        public DateTime CriadoEm { get; set; }
        public DateTime ExpiraEm { get; set; }
        public DateTime? RevogadoEm { get; set; }

        [NotMapped]
        public bool Ativo => RevogadoEm is null && DateTime.UtcNow < ExpiraEm;
    }
}
