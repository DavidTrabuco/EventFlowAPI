namespace EventFlow.Application.DTOs.Request
{
    public class AtualizarEventoRequest
    {
      
            public string Titulo { get; set; } = string.Empty;
            public string Descricao { get; set; } = string.Empty;
            public DateTime DataHora { get; set; }
            public string Local { get; set; } = string.Empty;
            public int CapacidadeMaxima { get; set; }
            public decimal PrecoIngresso { get; set; }

    }
}
