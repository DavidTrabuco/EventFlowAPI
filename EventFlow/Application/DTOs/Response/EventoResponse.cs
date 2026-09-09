using EventFlow.Domain.Entity;

namespace EventFlow.Application.DTOs.Response
{
    public class EventoResponse
    {
        public int Id { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public DateTime DataHora { get; set; }
        public string Local { get; set; } = string.Empty;
        public int CapacidadeMaxima { get; set; }
        public int IngressosVendidos { get; set; }
        public int VagasDisponiveis => CapacidadeMaxima - IngressosVendidos;
        public decimal PrecoIngresso { get; set; }
        public bool Ativo { get; set; }

        public static EventoResponse De(Evento evento) => new()
        {
            Id = evento.Id,
            Titulo = evento.Titulo,
            Descricao = evento.Descricao,
            DataHora = evento.DataHora,
            Local = evento.Local,
            CapacidadeMaxima = evento.CapacidadeMaxima,
            IngressosVendidos = evento.IngressosVendidos,
            PrecoIngresso = evento.PrecoIngresso,
            Ativo = evento.Ativo
        };
    }
}
