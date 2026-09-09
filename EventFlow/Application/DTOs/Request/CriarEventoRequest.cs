using System.ComponentModel.DataAnnotations;

namespace EventFlow.Application.DTOs.Request
{
    public class CriarEventoRequest
    {
        [Required(ErrorMessage = "O título é obrigatório.")]
        [MaxLength(150)]
        public string Titulo { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Descricao { get; set; } = string.Empty;

        [Required(ErrorMessage = "A data e hora são obrigatórias.")]
        public DateTime DataHora { get; set; }

        [Required(ErrorMessage = "O local é obrigatório.")]
        [MaxLength(200)]
        public string Local { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "A capacidade máxima deve ser maior que zero.")]
        public int CapacidadeMaxima { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "O preço do ingresso não pode ser negativo.")]
        public decimal PrecoIngresso { get; set; }
    }
}
