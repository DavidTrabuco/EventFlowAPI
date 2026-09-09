using EventFlow.Domain.Enums;

namespace EventFlow.Domain.Entity
{
    public class Ingresso
    {
        public int Id { get; set; }

        public int EventoId { get; set; }
        public Evento? Evento { get; set; }

        public int ParticipanteId { get; set; }
        public Participante? Participante { get; set; }

        public DateTime DataHoraCompra { get; set; }
        public decimal ValorPago { get; set; }
        public string CodigoValidacao { get; set; } = string.Empty;
        public StatusIngresso Status { get; set; } = StatusIngresso.Ativo;

        public Ingresso() { }
    }
}
