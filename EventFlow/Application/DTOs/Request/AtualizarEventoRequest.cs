using System.ComponentModel.DataAnnotations;



namespace EventFlow.Application.DTOs.Request
{
    public class AtualizarEventoRequest
    {
            [Required]
            [MinLength(1, ErrorMessage = "O campo 'Titulo' deve ter no mínimo 1 caractere.")]
            public string Titulo { get; set; } = string.Empty;
            public string Descricao { get; set; } = string.Empty;

            [Required(ErrorMessage = "O campo 'DataHora' é obrigatório.")]
            public DateTime? DataHora { get; set; }

            [Required(ErrorMessage = "O campo 'Local' é obrigatório.")]
            [MaxLength(150)]  
            public string Local { get; set; } = string.Empty;

            [Required(ErrorMessage = "O campo 'CapacidadeMaxima' é obrigatório.")]
            public int CapacidadeMaxima { get; set; }

            [Required(ErrorMessage = "O campo 'PrecoIngresso' é obrigatório.")]
            public decimal PrecoIngresso { get; set; }

    }
}
